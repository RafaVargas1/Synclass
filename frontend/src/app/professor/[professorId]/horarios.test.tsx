import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { TipoMarcacao } from '@/lib/api/horarios';
import { criarHorario, listarHorarios, removerHorario } from '@/lib/api/horarios';

import HorariosProfessorScreen from './horarios';

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/horarios', () => ({
  criarHorario: jest.fn(),
  listarHorarios: jest.fn(),
  removerHorario: jest.fn(),
  TipoMarcacao: { Livre: 0, Fixo: 1, Hibrido: 2 },
}));

const criarHorarioMock = criarHorario as jest.Mock;
const listarHorariosMock = listarHorarios as jest.Mock;
const removerHorarioMock = removerHorario as jest.Mock;

const horarioExistente = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  tipoMarcacao: TipoMarcacao.Livre,
};

async function selecionarPoliticaEEnviar() {
  await fireEvent.press(screen.getAllByRole('button', { name: 'Livre' })[0]);
  await fireEvent.press(screen.getByText('Adicionar horário'));
}

describe('HorariosProfessorScreen', () => {
  beforeEach(() => {
    criarHorarioMock.mockReset();
    listarHorariosMock.mockReset();
    removerHorarioMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioExistente] });
  });

  it('shows the horarios form directly on mount, without any gate, even without a ConfiguracaoProfessor', async () => {
    await render(<HorariosProfessorScreen />);

    expect(screen.getByText('Adicionar horário')).toBeTruthy();
    expect(screen.queryByText('Definir modelo')).toBeNull();
    await waitFor(() => expect(listarHorariosMock).toHaveBeenCalledWith('professor-1'));
  });

  it('loads and shows the existing horarios on mount', async () => {
    await render(<HorariosProfessorScreen />);

    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());
    expect(listarHorariosMock).toHaveBeenCalledWith('professor-1');
  });

  it('adds the created horario to the list on success', async () => {
    const novoHorario = {
      id: 'h2',
      diaSemana: 3,
      horaInicio: '09:00:00',
      duracaoMinutos: 30,
      tipoMarcacao: TipoMarcacao.Livre,
    };
    criarHorarioMock.mockResolvedValue({ sucesso: true, horario: novoHorario });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.changeText(screen.getByPlaceholderText('HH:mm'), '09:00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await selecionarPoliticaEEnviar();

    await waitFor(() => expect(screen.getByText(/Quarta/)).toBeTruthy());
    expect(criarHorarioMock).toHaveBeenCalledWith(
      'professor-1',
      expect.objectContaining({ tipoMarcacao: TipoMarcacao.Livre }),
    );
  });

  it('shows the Api error message when creation fails', async () => {
    criarHorarioMock.mockResolvedValue({ sucesso: false, mensagem: 'Horário conflita.' });
    await render(<HorariosProfessorScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.changeText(screen.getByPlaceholderText('HH:mm'), '14:00');
    await fireEvent.changeText(screen.getByPlaceholderText('60'), '30');
    await selecionarPoliticaEEnviar();

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
});
