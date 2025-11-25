import { Route, Routes } from 'react-router-dom';
import {
  UsersListView,
  UserCreateView,
  UserUpdateView } from 'features/users/views';
import { WithParams } from 'features/routing';

export const UsersRouter = () => (
  <Routes>
    <Route path="/" element={<UsersListView />}>
      <Route path="create" element={<UserCreateView />} />
      <Route path=":userId/update" element={<WithParams element={ UserUpdateView } />} />
    </Route>
  </Routes>
);
