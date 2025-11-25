import { UserCreateController } from 'features/users/controllers';
import { useNavigate } from 'react-router-dom';
import { RoutePaths } from 'features/routing';

export const UserCreateView = () => {
  const navigate = useNavigate();

  return (
    <UserCreateController.Modal
      isOpen={true}
      isDismissable={false}
      title="Create User"
      onSuccess={() => navigate(RoutePaths.users.list())}
      onClose={() => navigate(RoutePaths.users.list())}
    />
  );
};
