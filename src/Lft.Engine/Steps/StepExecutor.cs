using Lft.Discovery;
using Lft.Domain.Models;
using Lft.Domain.Services;
using Lft.Engine.Templates;
using Lft.Engine.Variables;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DomainInjectionPosition = Lft.Domain.Services.InjectionPosition;

namespace Lft.Engine.Steps;

public sealed class StepExecutor
{
    private readonly string _templatesRoot;
    private readonly ITemplateRenderer _renderer;
    private readonly IPathResolver? _pathResolver;
    private readonly ICodeInjector? _codeInjector;
    private readonly ILogger<StepExecutor> _logger;
    private ProjectManifest? _manifest;

    public StepExecutor(
        string templatesRoot,
        ITemplateRenderer renderer,
        IPathResolver? pathResolver = null,
        ICodeInjector? codeInjector = null,
        ILogger<StepExecutor>? logger = null)
    {
        _templatesRoot = templatesRoot;
        _renderer = renderer;
        _pathResolver = pathResolver;
        _codeInjector = codeInjector;
        _logger = logger ?? NullLogger<StepExecutor>.Instance;
    }

    /// <summary>
    /// Sets the project manifest for discovery-based path resolution.
    /// </summary>
    public void SetManifest(ProjectManifest manifest)
    {
        _manifest = manifest;
    }

    public async Task<IReadOnlyList<GeneratedFile>> ExecuteAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        CancellationToken ct = default)
    {
        var result = new List<GeneratedFile>();
        await ExecuteInternalAsync(step, request, vars, result, ct);
        return result;
    }

    private async Task ExecuteInternalAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        switch (step.Action.ToLowerInvariant())
        {
            case "group":
                foreach (var child in step.Steps)
                    await ExecuteInternalAsync(child, request, vars, files, ct);
                break;

            case "create":
                await ExecuteCreateAsync(step, request, vars, files, ct);
                break;

            case "inject":
                await ExecuteInjectAsync(step, request, vars, files, ct);
                break;

            case "ast-insert":
                await ExecuteAstInsertAsync(step, request, vars, files, ct);
                break;

            default:
                // Ignore unknown actions for now
                break;
        }
    }

    private async Task ExecuteCreateAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(step.Source))
            throw new InvalidOperationException($"Step '{step.Name}' of type 'create' must have a 'source'.");

        // 1. Read source template
        var sourcePath = Path.Combine(_templatesRoot, request.TemplatePack, step.Source);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Template source not found: {sourcePath}");
        }

        var templateContent = await File.ReadAllTextAsync(sourcePath, ct);

        // 2. Render content
        var rendered = _renderer.Render(templateContent, vars.AsReadOnly());

        // 3. Resolve output path
        var outputFileName = _renderer.Render(step.Output ?? "", vars.AsReadOnly());
        var varDict = vars.AsReadOnly();

        // Get profile root for discovery
        var profileRoot = varDict.TryGetValue("_ProfileRoot", out var rootVal) ? rootVal as string : null;

        string outputPath;

        if (!string.IsNullOrEmpty(profileRoot) && _pathResolver != null)
        {
            // Discovery-based path resolution
            outputPath = ResolvePathByDiscovery(outputFileName, profileRoot);
        }
        else
        {
            // Fallback: use output directly (no config found)
            outputPath = outputFileName;
        }

        files.Add(new GeneratedFile(outputPath, rendered));
    }

    private async Task ExecuteInjectAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        if (_codeInjector == null)
        {
            _logger.LogInformation("Skipping inject step '{Step}': No code injector configured.", step.Name);
            return;
        }

        if (string.IsNullOrEmpty(step.Template))
            throw new InvalidOperationException($"Step '{step.Name}' of type 'inject' must have a 'template' (code to inject).");

        var varDict = vars.AsReadOnly();
        var profileRoot = varDict.TryGetValue("_ProfileRoot", out var rootVal) ? rootVal as string : null;

        string? targetPath;
        string classSuffix;
        string methodName;
        DomainInjectionPosition position;

        // Check if using discovery-based target
        if (!string.IsNullOrEmpty(step.Target) && _manifest != null)
        {
            // Use discovered injection point
            var injectionPoint = GetInjectionPointByTarget(step.Target);
            if (injectionPoint == null)
            {
                _logger.LogInformation("Skipping inject '{Step}': Target '{Target}' not found in manifest", step.Name, step.Target);
                return;
            }

            targetPath = injectionPoint.FilePath;
            classSuffix = injectionPoint.ClassName;
            methodName = injectionPoint.MethodName;
            position = step.Position?.ToLowerInvariant() == "beginning"
                ? DomainInjectionPosition.Beginning
                : (step.Position?.ToLowerInvariant() == "end"
                    ? DomainInjectionPosition.End
                    : MapInjectionPosition(injectionPoint.DefaultPosition));
        }
        else
        {
            // Legacy mode: use explicit output, targetClass, targetMethod
            if (string.IsNullOrEmpty(step.Output))
                throw new InvalidOperationException($"Step '{step.Name}' of type 'inject' must have 'target' or 'output'.");

            if (string.IsNullOrEmpty(step.TargetClass) && string.IsNullOrEmpty(step.TargetMethod))
                throw new InvalidOperationException($"Step '{step.Name}' of type 'inject' must have 'targetClass' or 'targetMethod'.");

            var outputFileName = _renderer.Render(step.Output, varDict);
            targetPath = ResolveInjectTargetPath(outputFileName, profileRoot);
            classSuffix = _renderer.Render(step.TargetClass ?? "", varDict);
            methodName = _renderer.Render(step.TargetMethod ?? "", varDict);
            position = step.Position?.ToLowerInvariant() == "beginning"
                ? DomainInjectionPosition.Beginning
                : DomainInjectionPosition.End;
        }

        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
        {
            _logger.LogInformation("Skipping inject '{Step}': Target file not found", step.Name);
            return;
        }

        if (!_codeInjector.CanHandle(targetPath))
        {
            _logger.LogInformation("Skipping inject '{Step}': Injector cannot handle file type.", step.Name);
            return;
        }

        // Render the snippet template
        var snippet = _renderer.Render(step.Template, varDict);

        // Read current source
        var sourceCode = await File.ReadAllTextAsync(targetPath, ct);

        try
        {
            var context = new InjectionContext(
                sourceCode,
                classSuffix,
                methodName,
                snippet,
                position
            );

            var newSource = _codeInjector.Inject(context);

            if (newSource != sourceCode)
            {
                // Add as a modified file (will be written by the pipeline)
                files.Add(new GeneratedFile(targetPath, newSource, isModification: true));
                _logger.LogInformation("Injected snippet into {Target}", Path.GetFileName(targetPath));
            }
            else
            {
                _logger.LogInformation("Skipped injection (already exists): {Target}", Path.GetFileName(targetPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inject failed for '{Step}'", step.Name);
        }
    }

    private async Task ExecuteAstInsertAsync(
        TemplateStep step,
        GenerationRequest request,
        VariableContext vars,
        List<GeneratedFile> files,
        CancellationToken ct)
    {
        if (_codeInjector == null)
        {
            _logger.LogInformation("Skipping ast-insert step '{Step}': No code injector configured.", step.Name);
            return;
        }

        if (string.IsNullOrEmpty(step.Template))
            throw new InvalidOperationException($"Step '{step.Name}' of type 'ast-insert' must have a 'template' (code to inject).");

        var varDict = vars.AsReadOnly();
        var profileRoot = varDict.TryGetValue("_ProfileRoot", out var rootVal) ? rootVal as string : null;

        // Render output path (with Liquid variables)
        var outputFileName = _renderer.Render(step.Output ?? "", varDict);

        // Render the method/class names with Liquid
        var targetMethodRendered = _renderer.Render(step.TargetMethod ?? "", varDict);
        var targetClassRendered = _renderer.Render(step.TargetClass ?? "", varDict);

        // Resolve the target file path
        // If the output is an absolute path, use it directly; otherwise resolve relative
        string? targetPath;
        if (Path.IsPathRooted(outputFileName) && File.Exists(outputFileName))
        {
            targetPath = outputFileName;
        }
        else
        {
            targetPath = ResolveInjectTargetPath(outputFileName, profileRoot);
        }

        // If file doesn't exist and createFile is true, create it first
        if ((string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath)) && step.CreateFile && !string.IsNullOrEmpty(step.CreateFileSource))
        {
            // Create the file from template
            var createSourcePath = Path.Combine(_templatesRoot, request.TemplatePack, step.CreateFileSource);
            if (File.Exists(createSourcePath))
            {
                var templateContent = await File.ReadAllTextAsync(createSourcePath, ct);
                var rendered = _renderer.Render(templateContent, varDict);

                // Determine where to place the new file
                var newFilePath = ResolvePathByDiscovery(outputFileName, profileRoot ?? "");
                files.Add(new GeneratedFile(newFilePath, rendered));
                _logger.LogInformation("Created: {File}", Path.GetFileName(newFilePath));

                // Update target path to the newly created file path
                targetPath = newFilePath;
            }
            else
            {
                _logger.LogInformation(
                    "Skipping ast-insert '{Step}': CreateFileSource not found: {Source}",
                    step.Name,
                    step.CreateFileSource);
                return;
            }
        }

        if (string.IsNullOrEmpty(targetPath) || !File.Exists(targetPath))
        {
            _logger.LogInformation(
                "Skipping ast-insert '{Step}': Target file not found: {Output}",
                step.Name,
                outputFileName);
            return;
        }

        if (!_codeInjector.CanHandle(targetPath))
        {
            _logger.LogInformation("Skipping ast-insert '{Step}': Injector cannot handle file type.", step.Name);
            return;
        }

        // Render the snippet template
        var snippet = _renderer.Render(step.Template, varDict);

        // Read current source
        var sourceCode = await File.ReadAllTextAsync(targetPath, ct);

        // Check idempotency - if the code already exists, skip
        if (step.Idempotent && step.Parameters?.TryGetValue("queryPattern", out var queryPatternObj) == true)
        {
            var queryPattern = _renderer.Render(queryPatternObj?.ToString() ?? "", varDict);
            if (!string.IsNullOrEmpty(queryPattern) && sourceCode.Contains(queryPattern))
            {
                _logger.LogInformation("Skipped (already exists): {Query}", queryPattern);
                return;
            }
        }

        try
        {
            // Determine insertion type - InMethod uses targetMethod, InConstructor uses targetClass
            string classSuffix;
            string methodName;

            if (step.InsertionType?.Equals("InConstructor", StringComparison.OrdinalIgnoreCase) == true)
            {
                // For constructors, we need the class name and constructor
                classSuffix = targetClassRendered;
                methodName = targetClassRendered; // Constructor name = class name
            }
            else
            {
                // For methods, extract class from filename or use a default approach
                classSuffix = Path.GetFileNameWithoutExtension(targetPath);
                methodName = targetMethodRendered;
            }

            var position = step.Position?.ToLowerInvariant() == "beginning"
                ? DomainInjectionPosition.Beginning
                : DomainInjectionPosition.End;

            var context = new InjectionContext(
                sourceCode,
                classSuffix,
                methodName,
                snippet,
                position
            );

            var newSource = _codeInjector.Inject(context);

            if (newSource != sourceCode)
            {
                files.Add(new GeneratedFile(targetPath, newSource, isModification: true));
                _logger.LogInformation("Injected snippet into {Target}", Path.GetFileName(targetPath));
            }
            else
            {
                _logger.LogInformation("Skipped injection (no change): {Target}", Path.GetFileName(targetPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inject failed for '{Step}'", step.Name);
        }
    }

    private InjectionPoint? GetInjectionPointByTarget(string targetName)
    {
        if (_manifest == null)
            return null;

        if (Enum.TryParse<InjectionTarget>(targetName, ignoreCase: true, out var target))
        {
            return _manifest.GetInjectionPoint(target);
        }

        return null;
    }

    private static DomainInjectionPosition MapInjectionPosition(Lft.Discovery.InjectionPosition discoveryPosition)
    {
        return discoveryPosition == Lft.Discovery.InjectionPosition.Beginning
            ? DomainInjectionPosition.Beginning
            : DomainInjectionPosition.End;
    }

    private string? ResolveInjectTargetPath(string outputFileName, string? profileRoot)
    {
        if (string.IsNullOrEmpty(profileRoot))
            return null;

        var isFrontend = IsFrontendFile(outputFileName);
        var searchRoot = GetSearchRoot(profileRoot, isFrontend);

        // If output contains path segments, search for files matching the end pattern
        var fileName = Path.GetFileName(outputFileName);
        var searchPattern = $"*{fileName}";

        try
        {
            var matches = Directory.GetFiles(searchRoot, searchPattern, SearchOption.AllDirectories);

            // If output has directory parts, filter by path suffix
            if (outputFileName.Contains('/') || outputFileName.Contains(Path.DirectorySeparatorChar))
            {
                var pathSuffix = outputFileName.Replace('/', Path.DirectorySeparatorChar);
                matches = matches.Where(m => m.EndsWith(pathSuffix, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            return matches.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private string ResolvePathByDiscovery(string outputFileName, string profileRoot)
    {
        // Determine if this is frontend or backend based on file extension
        var isFrontend = IsFrontendFile(outputFileName);
        var searchRoot = GetSearchRoot(profileRoot, isFrontend);
        var searchSuffix = GetFileSuffix(outputFileName);

        // Extract just the file name (without subdirectories from template output)
        var justFileName = Path.GetFileName(outputFileName);
        var templateSubDir = Path.GetDirectoryName(outputFileName);

        if (_pathResolver != null && !string.IsNullOrEmpty(searchSuffix))
        {
            var resolution = _pathResolver.Resolve(searchSuffix, searchRoot);
            if (resolution != null)
            {
                string finalPath;

                if (isFrontend)
                {
                    // Frontend: use discovered src root + template structure (features/xxx/...)
                    var srcRoot = FindSrcRoot(searchRoot);
                    finalPath = Path.Combine(srcRoot, outputFileName);
                }
                else if (!string.IsNullOrEmpty(templateSubDir))
                {
                    // Backend with subdir (e.g., Entities/Entity.cs): discovered base + subdir + file
                    // But avoid duplication if discovered directory already ends with the first segment
                    var firstSegment = templateSubDir.Split(Path.DirectorySeparatorChar, '/')[0];
                    var discoveredDirName = Path.GetFileName(resolution.Directory);

                    if (string.Equals(firstSegment, discoveredDirName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Directory already matches - just use file name (or remaining subdir if nested)
                        var remainingPath = templateSubDir.Length > firstSegment.Length
                            ? templateSubDir.Substring(firstSegment.Length + 1)
                            : "";

                        finalPath = string.IsNullOrEmpty(remainingPath)
                            ? Path.Combine(resolution.Directory, justFileName)
                            : Path.Combine(resolution.Directory, remainingPath, justFileName);
                    }
                    else
                    {
                        finalPath = Path.Combine(resolution.Directory, templateSubDir, justFileName);
                    }
                }
                else
                {
                    // Backend without subdir: discovered base + file
                    finalPath = Path.Combine(resolution.Directory, justFileName);
                }

                _logger.LogInformation("Discovered {Directory}/ → {Output}", Path.GetFileName(resolution.Directory), outputFileName);
                return finalPath;
            }
        }

        // Fallback: use appropriate root + template output path
        if (isFrontend)
        {
            var srcRoot = FindSrcRoot(searchRoot);
            return Path.Combine(srcRoot, outputFileName);
        }

        return Path.Combine(searchRoot, outputFileName);
    }

    private static bool IsFrontendFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".js" or ".jsx" or ".ts" or ".tsx" or ".vue" or ".svelte";
    }

    private static string GetSearchRoot(string profileRoot, bool isFrontend)
    {
        if (isFrontend)
        {
            // Frontend: look in app/ subdirectory if it exists
            var appPath = Path.Combine(profileRoot, "app");
            if (Directory.Exists(appPath))
                return appPath;
        }
        else
        {
            // Backend: look in api/ subdirectory if it exists
            var apiPath = Path.Combine(profileRoot, "api");
            if (Directory.Exists(apiPath))
                return apiPath;
        }

        return profileRoot;
    }

    private static string FindSrcRoot(string searchRoot)
    {
        // Look for common frontend source directories
        var srcPath = Path.Combine(searchRoot, "src");
        if (Directory.Exists(srcPath))
            return srcPath;

        return searchRoot;
    }

    private static string? GetFileSuffix(string fileName)
    {
        // Extract meaningful suffix patterns
        var suffixes = new[]
        {
            "Model.cs", "Entity.cs", "Repository.cs", "Service.cs",
            "Interface.cs", "Endpoint.cs", "Controller.cs", "Routes.cs",
            ".jsx", ".js", ".ts", ".tsx"
        };

        foreach (var suffix in suffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return suffix;
            }
        }

        // Generic: return extension
        return Path.GetExtension(fileName);
    }
}
