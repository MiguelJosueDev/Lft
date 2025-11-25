import { useState } from 'react';
import { Reactive, useReactorOnSuccess } from '@livefree/reactive';
import { DeleteButton, SaveButton, withModalController } from '@livefree/react-ui';
import { UserForm } from 'features/users/components';

const UserUpdateController = Reactive(
  ({ services, data, monitors, userId, onSuccess, onClose }) => {
    const { user } = data;
    const [form, setForm] = useState({});

    useReactorOnSuccess(services.users, 'updateUser', payload => {
      onSuccess && onSuccess(payload);
    });

    useReactorOnSuccess(services.users, 'deleteUser', () => {
      onClose && onClose();
    });

    return (
      <div className="flex flex-col gap-2">
        <UserForm user={ user } onChange={setForm} />
        <div className="flex justify-between gap-2">
          <DeleteButton
            size="sm"
            onPress={() => services.users.deleteUser(userId)}
            isLoading={monitors.deleteUser.executing}
          />
          <SaveButton
            size="sm"
            isDisabled={!form.isValid}
            onPress={() =>
              services.users.updateUser(userId, form.data)
            }
            isLoading={monitors.updateUser.executing}
          />
        </div>
      </div>
    );
  },
  {
    name: 'UserUpdateController',
    sync: (services, { userId }) => [
      {
        execute: () => services.users.getUser(userId),
        deps: [userId],
      },
    ],
    queries: ({ userId }) => [
      {
        name: 'user',
        defaultValue: {},
        execute: db => db.users.get(userId),
        deps: [userId],
      },
    ],
    monitors: services => [
      services.users.updateUser,
      services.users.deleteUser,
    ],
  },
);

UserUpdateController.Modal = withModalController(({ onSuccess, ...props }) => (
  <UserUpdateController onSuccess={onSuccess} {...props} />
));

export { UserUpdateController };
