import { useState } from 'react';
import { Button } from '@heroui/react';
import { Reactive, useReactorOnSuccess } from '@livefree/reactive';
import { SaveButton, withModalController } from '@livefree/react-ui';
import { UserForm } from 'features/users/components';

const UserCreateController = Reactive(
  ({ services, monitors, onSuccess, onClose }) => {
    const [form, setForm] = useState({});

    useReactorOnSuccess(services.users, 'createUser', payload => {
      onSuccess && onSuccess(payload);
    });

    return (
      <div className="flex flex-col gap-2">
        <UserForm onChange={setForm} />
        <div className="flex justify-between gap-2">
          <Button size="sm" onPress={() => onClose && onClose()} color="default" variant="light">
            Cancel
          </Button>
          <SaveButton
            size="sm"
            isDisabled={!form.isValid}
            onPress={() =>
              services.users.createUser(form.data)
            }
            isLoading={monitors.createUser.executing}
          />
        </div>
      </div>
    );
  },
  {
    name: 'UserCreateController',
    monitors: services => [services.users.createUser],
  },
);

UserCreateController.Modal = withModalController(({ onSuccess, ...props }) => (
  <UserCreateController onSuccess={onSuccess} {...props} />
));

export { UserCreateController };
