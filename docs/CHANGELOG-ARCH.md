# LFT – Changelog de Arquitectura

Este documento registra los cambios significativos en los contratos compartidos (`Lft.Domain`, `Lft.Abstractions`) y decisiones de arquitectura que impactan a múltiples módulos.

## Plantilla de entrada
- **Fecha**: YYYY-MM-DD
- **Autor/Agente**: Nombre
- **Cambio**: Descripción breve.
- **Impacto**: Módulos afectados.
- **Notas**: Detalles de migración.

---

## 2025-11-24
- **Autor**: Architect Agent
- **Cambio**: Definición inicial de la arquitectura en capas y creación del esqueleto de documentación.
- **Impacto**: Todos.
- **Notas**: Establecimiento de la estructura base `/docs`.

## 2025-11-24
- **Autor**: Engine Agent
- **Cambio**: Introducción de `TemplatePack` y `TemplateStep` en `Lft.Engine`.
- **Impacto**: `Lft.Engine`, `Lft.Cli`.
- **Notas**: Soporte para packs de templates y ejecución por pasos.
