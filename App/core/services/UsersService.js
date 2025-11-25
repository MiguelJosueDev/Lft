import { ReactiveService } from '@livefree/reactive';
import { GeneratedSDK } from 'sdk/GeneratedSDK';
import { GeneratedConfig } from 'core/config';
const UsersClient = GeneratedSDK.UsersClient;

export const UsersService = new (ReactiveService('users', UsersClient, {
  recursive: true,
  recurseDepth: 2,
}))(GeneratedConfig);

export const UsersReactor = UsersService.reactor('usersReactor', {
  success: (response, services, db) => {
    const { source, args, payload } = response;

    switch (source) {

      case UsersService.getUser:
        return db.users.put(payload);

      case UsersService.createUser:
        services.alerts.actionSuccess(`User added successfully`, 'users', response);
        return db.users.add(payload);

      case UsersService.updateUser:
        services.alerts.actionSuccess(`User updated successfully`, 'users', response);
        return db.users.put(payload);

      case UsersService.deleteUser:
        services.alerts.actionSuccess(`User deleted successfully`, 'users', response);
        return db.users.delete(args[0]);
    }
  },
  error: (response, services) => {
    switch (response.source) {

      case UsersService.getUser:
        return services.alerts.actionError(`Failed to fetch User details`, 'users', response);

      case UsersService.createUser:
        return services.alerts.actionError(`Failed to create User`, 'users', response);

      case UsersService.updateUser:
        return services.alerts.actionError(`Failed to update User`, 'users', response);

      case UsersService.deleteUser:
        return services.alerts.actionError(`Failed to delete User`, 'users', response);
    }
  },
});
