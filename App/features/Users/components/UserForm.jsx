import { Uniformly } from '@livefree/uniformly';
import { FieldStatus, TextField } from '@livefree/react-ui';

export const UserForm = ({ user = {}, onChange }) => (
  <Uniformly
    className="flex flex-col flex-grow gap-2"
    onChange={onChange}
  >
  </Uniformly>
);
