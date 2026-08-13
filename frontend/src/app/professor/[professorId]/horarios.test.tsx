import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { definirModeloAgendamento, ModeloAgendamento, obterConfiguracao } from '@/lib/api/configuracao';
import { criarHorario, listarHorarios, removerHorario } from '@/lib/api/horarios';

import HorariosProfessorScreen from './horarios';

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

jest.mock('@/lib/api/horarios', () => ({
  criarHorario: jest.fn(),
  listarHorarios: jest.fn(),
  removerHorario: jest.fn(),
}));

jest.mock('@/lib/api/configuracao', () => {
  const actual = jest.requireActual('@/lib/api/configuracao');
  return {
    ...actual,
    definirModeloAgendamento: jest.fn(),
    obterConfiguracao: jest.fn(),
  };
});

const criarHorarioMock = criarHorario as jest.Mock;
const listarHorariosMock = listarHorarios as jest.Mock;
const removerHorarioMock = removerHorario as jest.Mock;
const obterConfiguracaoMock = obterConfiguracao as jest.Mock;
const definirModeloAgendamentoMock = definirModeloAgendamento as jest.Mock;

const horarioExistente = { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 };

describe('HorariosProfessorScreen', () => {
  beforeEach(() => {
    criarHorarioMock.mockReset();
    listarHorariosMock.mockReset();
    removerHorarioMock.mockReset();
    obterConfiguracaoMock.mockReset();
    definirModeloAgendamentoMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioExistente] });
    obterConfiguracaoMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Vago,
    });
  });

  it('loads and shows the existing horarios on mount when a modelo is already defined', async () => {
    await render(<HorariosProfessorScreen />);

    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());
    expect(listarHorariosMock).toHaveBeenCalledWith('professor-1');
  });

  it('adds the created horario to the list on success', async () => {
    const novoHorario = { id: 'h2', diaSemana: 3, horaInicio: '09:00:00', duracaoMinutos: 30 };
    criarHorarioMock.mockResolvedValue({ sucesso: true, horario: novoHorario });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.changeText(screen.getByPlaceholderText('HH:mm'), '09:00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await fireEvent.press(screen.getByText('Adicionar horário'));

    await waitFor(() => expect(screen.getByText(/Quarta/)).toBeTruthy());
  });

  it('shows the Api error message when creation fails', async () => {
    criarHorarioMock.mockResolvedValue({ sucesso: false, mensagem: 'Horário conflita.' });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.changeText(screen.getByPlaceholderText('HH:mm'), '14:00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await fireEvent.press(screen.getByText('Adicionar horário'));

    await waitFor(() => expect(screen.getByText('Horário conflita.')).toBeTruthy());
  });

  it('removes the horario from the list when removal succeeds', async () => {
    removerHorarioMock.mockResolvedValue({ sucesso: true });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    await waitFor(() => expect(screen.queryByText(/Terça/)).toBeNull());
    expect(removerHorarioMock).toHaveBeenCalledWith('professor-1', 'h1');
  });

  it('shows the Api error message and keeps the horario when removal fails', async () => {
    removerHorarioMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Não é possível remover: existem Alunos alocados.',
    });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Remover'));

    await waitFor(() =>
      expect(screen.getByText('Não é possível remover: existem Alunos alocados.')).toBeTruthy(),
    );
    expect(screen.getByText(/Terça/)).toBeTruthy();
  });

  describe('gate de modelo de agendamento (issue #7)', () => {
    it('shows ModeloAgendamentoForm instead of the horarios screen when the configuracao is not defined (404)', async () => {
      obterConfiguracaoMock.mockResolvedValue({ sucesso: true, definida: false });

      await render(<HorariosProfessorScreen />);

      await waitFor(() => expect(screen.getByText('Definir modelo')).toBeTruthy());
      expect(screen.queryByText('Adicionar horário')).toBeNull();
      expect(listarHorariosMock).not.toHaveBeenCalled();
    });

    it('releases the normal screen without reloading once the modelo is defined', async () => {
      obterConfiguracaoMock.mockResolvedValue({ sucesso: true, definida: false });
      definirModeloAgendamentoMock.mockResolvedValue({
        sucesso: true,
        modeloAgendamento: ModeloAgendamento.Vago,
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(screen.getByText('Definir modelo')).toBeTruthy());

      await fireEvent.press(screen.getByText('Definir modelo'));

      await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());
      expect(obterConfiguracaoMock).toHaveBeenCalledTimes(1);
      expect(listarHorariosMock).toHaveBeenCalledWith('professor-1');
    });

    it('shows the Api error message when defining the modelo fails, keeping the form', async () => {
      obterConfiguracaoMock.mockResolvedValue({ sucesso: true, definida: false });
      definirModeloAgendamentoMock.mockResolvedValue({
        sucesso: false,
        mensagem: 'Não foi possível concluir a operação.',
      });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(screen.getByText('Definir modelo')).toBeTruthy());

      await fireEvent.press(screen.getByText('Definir modelo'));

      await waitFor(() =>
        expect(screen.getByText('Não foi possível concluir a operação.')).toBeTruthy(),
      );
      expect(screen.getByText('Definir modelo')).toBeTruthy();
    });
  });

  describe('falha ao carregar a configuração (achado do dev-review/qa-review, PR #26)', () => {
    it('shows a loading indicator instead of a blank screen while obterConfiguracao is pending', async () => {
      let resolver: (value: unknown) => void = () => {};
      obterConfiguracaoMock.mockReturnValue(new Promise((resolve) => (resolver = resolve)));

      await render(<HorariosProfessorScreen />);

      expect(screen.getByLabelText('Carregando')).toBeTruthy();

      resolver({ sucesso: true, definida: true, modeloAgendamento: ModeloAgendamento.Vago });
      await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());
    });

    it('shows an error message with a retry action instead of a blank screen forever when obterConfiguracao fails', async () => {
      obterConfiguracaoMock.mockResolvedValue({
        sucesso: false,
        mensagem: 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.',
      });

      await render(<HorariosProfessorScreen />);

      await waitFor(() =>
        expect(
          screen.getByText('Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.'),
        ).toBeTruthy(),
      );
      expect(screen.getByText('Tentar novamente')).toBeTruthy();
    });

    it('retries obterConfiguracao and shows the right screen when the retry succeeds', async () => {
      obterConfiguracaoMock
        .mockResolvedValueOnce({ sucesso: false, mensagem: 'Erro de conexão.' })
        .mockResolvedValueOnce({ sucesso: true, definida: false });
      await render(<HorariosProfessorScreen />);
      await waitFor(() => expect(screen.getByText('Tentar novamente')).toBeTruthy());

      await fireEvent.press(screen.getByText('Tentar novamente'));

      await waitFor(() => expect(screen.getByText('Definir modelo')).toBeTruthy());
      expect(obterConfiguracaoMock).toHaveBeenCalledTimes(2);
    });
  });
});
