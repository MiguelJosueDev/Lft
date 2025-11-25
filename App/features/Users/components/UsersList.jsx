import { useCallback } from 'react';
import { TableRow, TableColumn, TableCell } from '@heroui/react';;
import { RequestBrokerDataTable } from '@livefree/react-ui';

const columns = [
  { propertyName: 'id', name: 'Id', isSortable: true },
];

export const UsersList = ({ users = [], filters, onSelect }) => {
  const renderCell = useCallback((row, columnKey) => {
    const cellValue = row[columnKey];

    switch (columnKey) {
      default:
        return cellValue;
    }
  }, []);

  return (
    <RequestBrokerDataTable
      columns={columns}
      items={ users }
      filters={filters}
      renderColumn={column => (
        <TableColumn
          key={column.propertyName}
          allowsSorting={column?.isSortable}
        >
          {column.name}
        </TableColumn>
      )}
      renderRow={row => (
        <TableRow key={row.id} onClick={() => onSelect(row)}>
          {columnKey => <TableCell key={columnKey}>{renderCell(row, columnKey)}</TableCell>}
        </TableRow>
      )}
      withPagination
      withSorting
      withColumnSelection
    />
  );
};
