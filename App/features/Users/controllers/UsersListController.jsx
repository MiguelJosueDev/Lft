import { useCallback } from 'react';
import { Reactive } from '@livefree/reactive';
import { RequestBroker, queryDexieWithMetadata } from '@livefree/applications';
import { UsersList } from 'features/users/components';

export const UsersListController = Reactive(
  ({ data, services, monitors, db, onSelect }) => {
    const { users } = data;

    const handleExecute = useCallback((action, metadata) => {
      action(metadata);
    }, []);

    return (
      <RequestBroker
        actionKey="queryUsers"
        action={services.users.queryUsers}
        defaultSorting={ { propertyName: 'id', sortType: false } }
        defaultPaging={ { skip: 0, take: 50, page: 1, totalCount: 0 } }
        isExecuting={monitors.queryUsers.executing}
        skipInitial={() => db.users.toCollection().count()}
        onExecute={handleExecute}
      >
        <UsersList
          users={ users }
          isExecuting={monitors.queryUsers.executing}
          onSelect={item => onSelect && onSelect(item)}
        />
      </RequestBroker>
    );
  },
  {
    name: 'UsersListController',
    queries: (_, services) => [
      {
        name: 'users',
        execute: db =>
          queryDexieWithMetadata(db, services.users.queryUsers.action, () => db.users, {
            sorting: true,
          }),
        deps: [],
      },
    ],
    monitors: services => [services.users.queryUsers],
  },
);
