import { Outlet, useNavigate } from 'react-router-dom';
import { Breadcrumbs, BreadcrumbItem } from '@heroui/react';
import { useSyncCreateItem } from '@livefree/applications';
import { UsersListController } from 'features/users/controllers';
import { RoutePaths } from 'features/routing';

export const UsersListView = () => {
  const navigate = useNavigate();
  useSyncCreateItem('user');

  return (
    <>
      <div className="flex flex-row justify-between mb-4">
        <Breadcrumbs className="content-center">
          <BreadcrumbItem isDisabled>Generated</BreadcrumbItem>
          <BreadcrumbItem>Users</BreadcrumbItem>
        </Breadcrumbs>
      </div>
      <UsersListController onSelect={item => navigate(RoutePaths.users.update(item.id))} />
      <Outlet />
    </>
  );
};
