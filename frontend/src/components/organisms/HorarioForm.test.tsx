import { fireEvent, render, screen } from '@testing-library/react-native';

import { TipoMarcacao } from '@/lib/api/horarios';

import { HorarioForm } from './HorarioForm';

async function selecionarHora(hora: string, minuto: string) {
  await fireEvent.changeText(screen.getByLabelText('Hora de início'), `${hora}${minuto}`);
}

async function preencherEEnviar(hora: string, minuto: string, duracaoMinutos: string) {
  await fireEvent.press(screen.getByText('Livre'));
  await selecionarHora(hora, minuto);
  await fireEvent.changeText(screen.getByPlaceholderText('60'), duracaoMinutos);
  await fireEvent.press(screen.getByText('Adicionar horário'));
}

describe('HorarioForm', () => {
  it('uses SeletorDeHora as a masked HH:mm text field for the start time', async () => {
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={jest.fn()} />);

    expect(screen.getByText('Hora de início')).toBeTruthy();
    expect(screen.getByPlaceholderText('HH:mm')).toBeTruthy();
    expect(screen.queryByText('Selecionar hora')).toBeNull();
  });

  it('calls onSubmit with valid data and no conflict', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await preencherEEnviar('10', '00', '60');

    expect(onSubmit).toHaveBeenCalledWith({
      diaSemana: 1,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 1,
      tipoMarcacao: TipoMarcacao.Livre,
    });
  });

  it('calls onSubmit with the selected tipoMarcacao when a chip other than Livre is chosen', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await fireEvent.press(screen.getByText('Híbrido'));
    await selecionarHora('10', '00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '60');
    await fireEvent.press(screen.getByText('Adicionar horário'));

    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ tipoMarcacao: TipoMarcacao.Hibrido }),
    );
  });

  it('shows a client-side error and does not call onSubmit when no política is chosen', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await selecionarHora('10', '00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '60');
    await fireEvent.press(screen.getByText('Adicionar horário'));

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('Escolha a política de marcação deste horário.')).toBeTruthy();
  });

  it('calls onSubmit with the informed limiteAlunos when the default is changed', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await fireEvent.changeText(screen.getByPlaceholderText('1'), '4');
    await preencherEEnviar('10', '00', '60');

    expect(onSubmit).toHaveBeenCalledWith({
      diaSemana: 1,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 4,
      tipoMarcacao: TipoMarcacao.Livre,
    });
  });

  it('shows a client-side error and does not call onSubmit when limiteAlunos is zero or negative', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await fireEvent.changeText(screen.getByPlaceholderText('1'), '0');
    await preencherEEnviar('10', '00', '60');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText('Informe um limite de alunos maior que zero.')).toBeTruthy();
  });

  it('shows a client-side error and does not call onSubmit when the new horario overlaps an existing one', async () => {
    const onSubmit = jest.fn();
    const horariosExistentes = [
      {
        id: 'h1',
        diaSemana: 1,
        horaInicio: '10:00:00',
        duracaoMinutos: 60,
        limiteAlunos: 1,
        tipoMarcacao: TipoMarcacao.Livre,
        prazoCancelamentoMinutos: 0,
      },
    ];
    await render(
      <HorarioForm horariosExistentes={horariosExistentes} enviando={false} onSubmit={onSubmit} />,
    );

    await preencherEEnviar('10', '30', '30');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/conflita/)).toBeTruthy();
  });

  it('shows a client-side error and does not call onSubmit when duration is invalid', async () => {
    const onSubmit = jest.fn();
    await render(<HorarioForm horariosExistentes={[]} enviando={false} onSubmit={onSubmit} />);

    await preencherEEnviar('10', '00', '0');

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
