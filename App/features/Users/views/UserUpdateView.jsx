import { UserUpdateController } from 'features/users/controllers';
import { useNavigate } from 'react-router-dom';
import { RoutePaths } from 'features/routing';

export const UserUpdateView = ({ userId }) => {
  const parsedId =
    !isNaN(Number(userId)) && userId !== '' ? Number(userId) : userId;
  const navigate = useNavigate();

  return (
    <UserUpdateController.Modal
      isOpen={true}
      isDismissable={false}
      title="Update User"
      userId={parsedId}
      onSuccess={() => navigate(RoutePaths.users.list())}
      onClose={() => navigate(RoutePaths.users.list())}
    />
  );
};
