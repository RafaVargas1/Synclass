import { fireEvent, render, screen } from '@testing-library/react-native';

import { ModeloAgendamento } from '@/lib/api/configuracao';

import { ModeloAgendamentoForm } from './ModeloAgendamentoForm';

describe('ModeloAgendamentoForm', () => {
  it('calls onSubmit with the selected modelo (Fixo)', async () => {
    const onSubmit = jest.fn();
    await render(<ModeloAgendamentoForm enviando={false} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Fixo'));
    await fireEvent.press(screen.getByText('Definir modelo'));

    expect(onSubmit).toHaveBeenCalledWith(ModeloAgendamento.Fixo);
  });

  it('calls onSubmit with the selected modelo (Híbrido)', async () => {
    const onSubmit = jest.fn();
    await render(<ModeloAgendamentoForm enviando={false} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Híbrido'));
    await fireEvent.press(screen.getByText('Definir modelo'));

    expect(onSubmit).toHaveBeenCalledWith(ModeloAgendamento.Hibrido);
  });

  it('defaults to Vago and disables the button while enviando', async () => {
    const onSubmit = jest.fn();
    await render(<ModeloAgendamentoForm enviando={true} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Salvando...'));

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('shows the Api error message passed via prop', async () => {
    await render(
      <ModeloAgendamentoForm
        enviando={false}
        erro="Não foi possível concluir a operação."
        onSubmit={jest.fn()}
      />,
    );

    expect(screen.getByText('Não foi possível concluir a operação.')).toBeTruthy();
  });
});
