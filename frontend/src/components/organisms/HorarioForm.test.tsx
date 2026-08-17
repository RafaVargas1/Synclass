import { fireEvent, render, screen } from '@testing-library/react-native';

import { HorarioForm } from './HorarioForm';

async function preencherEEnviar(horaInicio: string, duracaoMinutos: string) {
  await fireEvent.changeText(screen.getByPlaceholderText('HH:mm'), horaInicio);
  await fireEvent.changeText(screen.getByPlaceholderText('60'), duracaoMinutos);
  await fireEvent.press(screen.getByText('Adicionar horário'));
}

describe('HorarioForm', () => {
  it('calls onSubmit with valid data and no conflict', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await preencherEEnviar('10:00', '60');

    expect(onSubmit).toHaveBeenCalledWith({
      diaSemana: 1,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 1,
    });
  });

  it('calls onSubmit with the informed limiteAlunos when the default is changed', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await fireEvent.changeText(screen.getByPlaceholderText('1'), '4');
    await preencherEEnviar('10:00', '60');

    expect(onSubmit).toHaveBeenCalledWith({
      diaSemana: 1,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 4,
    });
  });

  it('shows a client-side error and does not call onSubmit when limiteAlunos is zero or negative', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await fireEvent.changeText(screen.getByPlaceholderText('1'), '0');
    await preencherEEnviar('10:00', '60');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('Informe um limite de alunos maior que zero.')).toBeTruthy();
  });

  it('shows a client-side error and does not call onSubmit when the new horario overlaps an existing one', async () => {
    const onSubmit = jest.fn();
    const horariosExistentes = [
      { id: 'h1', diaSemana: 1, horaInicio: '10:00:00', duracaoMinutos: 60, limiteAlunos: 1 },
    ];
    await render(
      <HorarioForm horariosExistentes={horariosExistentes} enviando={false} onSubmit={onSubmit} />,
    );

    await preencherEEnviar('10:30', '30');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/conflita/)).toBeTruthy();
  });

  it('shows a client-side error and does not call onSubmit when duration is invalid', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await preencherEEnviar('10:00', '0');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('Informe uma duração em minutos maior que zero.')).toBeTruthy();
  });

  it('shows the Api error message passed via prop', async () => {
    await render(
      <HorarioForm
        horariosExistentes={[]}
        enviando={false}
        erro="Não foi possível concluir a operação."
        onSubmit={jest.fn()}
      />,
    );

    expect(screen.getByText('Não foi possível concluir a operação.')).toBeTruthy();
  });
});
