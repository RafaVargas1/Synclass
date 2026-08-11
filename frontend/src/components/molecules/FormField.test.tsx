import { render, screen } from '@testing-library/react-native';

import { FormField } from '@/components/molecules/FormField';

describe('FormField', () => {
  it('renders the label and the input value', async () => {
    await render(<FormField label="Nome" value="Maria" onChangeText={() => {}} />);

    expect(screen.getByText('Nome')).toBeTruthy();
    expect(screen.getByDisplayValue('Maria')).toBeTruthy();
  });

  it('does not render an error message when none is given', async () => {
    await render(<FormField label="Nome" value="" onChangeText={() => {}} />);

    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('renders the error message when given', async () => {
    await render(
      <FormField
        label="Contato"
        value=""
        onChangeText={() => {}}
        errorMessage="Contato inválido."
      />,
    );

    expect(screen.getByRole('alert')).toHaveTextContent('Contato inválido.');
  });
});
