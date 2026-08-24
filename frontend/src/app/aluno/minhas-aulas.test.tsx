import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cancelarAula, listarProximasAulas } from '@/lib/api/cancelamentos';
import { confirmarPresenca } from '@/lib/api/frequencias';
import { listarHorariosVagos, marcarHorario } from '@/lib/api/marcacoes';
import { listarVinculosAluno } from '@/lib/api/vinculosAluno';

import MinhasAulasAlunoScreen from './minhas-aulas';

jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('@/lib/api/vinculosAluno', () => ({ listarVinculosAluno: jest.fn() }));
jest.mock('@/lib/api/cancelamentos', () => ({
  listarProximasAulas: jest.fn(),
  cancelarAula: jest.fn(),
}));
jest.mock('@/lib/api/marcacoes', () => ({
  listarHorariosVagos: jest.fn(),
  marcarHorario: jest.fn(),
}));
jest.mock('@/lib/api/frequencias', () => ({ confirmarPresenca: jest.fn() }));

const listarVinculosAlunoMock = listarVinculosAluno as jest.Mock;
const listarProximasAulasMock = listarProximasAulas as jest.Mock;
const cancelarAulaMock = cancelarAula as jest.Mock;
const listarHorariosVagosMock = listarHorariosVagos as jest.Mock;
const marcarHorarioMock = marcarHorario as jest.Mock;
const confirmarPresencaMock = confirmarPresenca as jest.Mock;

const professorUnico = [{ professorId: 'prof-1', nome: 'Ana' }];
const doisProfessores = [
  { professorId: 'prof-1', nome: 'Ana' },
  { professorId: 'prof-2', nome: 'Bruno' },
];

const aulaMarcada = {
  horarioId: 'h1',
  data: '2026-08-25',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  podeCancelar: true,
  cancelavelAte: '2026-08-25T08:00:00',
  prazoCancelamentoMinutos: 120,
};

const horarioVago = {
  id: 'hv1',
  diaSemana: 3,
  horaInicio: '14:00:00',
  duracaoMinutos: 60,
  vagasRestantes: 2,
};

describe('MinhasAulasAlunoScreen', () => {
  beforeEach(() => {
    listarVinculosAlunoMock.mockReset();
    listarProximasAulasMock.mockReset();
    cancelarAulaMock.mockReset();
    listarHorariosVagosMock.mockReset();
    marcarHorarioMock.mockReset();
    confirmarPresencaMock.mockReset();
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });
    listarHorariosVagosMock.mockResolvedValue({ sucesso: true, horarios: [] });
  });

  it('shows an error message when listarVinculosAluno fails', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: false, mensagem: 'Falha ao carregar.' });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Falha ao carregar.')).toBeTruthy());
  });

  it('shows an empty-state message when the Aluno has no Professor vinculado', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() =>
      expect(screen.getByText('Você ainda não está vinculado a nenhum Professor.')).toBeTruthy(),
    );
  });

  it('does not show a professor selector when there is only one vínculo', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(listarProximasAulasMock).toHaveBeenCalledWith('prof-1'));
    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('shows a professor selector when there is more than one vínculo, and switches data on selection', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: doisProfessores });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(listarProximasAulasMock).toHaveBeenCalledWith('prof-1'));
    expect(screen.getByRole('button', { name: 'Ana' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Bruno' })).toBeTruthy();

    await fireEvent.press(screen.getByRole('button', { name: 'Bruno' }));

    await waitFor(() => expect(listarProximasAulasMock).toHaveBeenCalledWith('prof-2'));
  });

  it('shows the empty-state messages for both sections when there is nothing to show', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() =>
      expect(screen.getByText('Você ainda não tem nenhuma aula marcada.')).toBeTruthy(),
    );
    expect(screen.getByText('Nenhum horário disponível para marcação no momento.')).toBeTruthy();
  });

  it('lists Minhas aulas grouped, and confirms presença directly (no modal for confirming presença)', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [aulaMarcada] });
    confirmarPresencaMock.mockResolvedValue({
      sucesso: true,
      confirmacao: { aulaId: 'a1', matriculaId: 'm1', confirmadoPeloAluno: true },
    });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(screen.getByText(/Terça, 25\/08/)).toBeTruthy());

    await fireEvent.press(screen.getByRole('button', { name: /Confirmar presença/ }));

    await waitFor(() => expect(confirmarPresencaMock).toHaveBeenCalledWith('prof-1', 'h1', '2026-08-25'));
  });

  it('opens a confirmation modal before cancelling an aula, and only calls cancelarAula after confirming', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [aulaMarcada] });
    cancelarAulaMock.mockResolvedValue({
      sucesso: true,
      cancelamento: { id: 'c1', aulaId: 'a1', matriculaId: 'm1', canceladoEm: '2026-08-20T10:00:00' },
    });

    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Terça, 25\/08/)).toBeTruthy());

    await fireEvent.press(screen.getByRole('button', { name: /Cancelar/ }));
    expect(cancelarAulaMock).not.toHaveBeenCalled();
    expect(screen.getByText('Confirmar cancelamento')).toBeTruthy();

    await fireEvent.press(screen.getByRole('button', { name: 'Cancelar aula' }));

    await waitFor(() => expect(cancelarAulaMock).toHaveBeenCalledWith('prof-1', 'h1', '2026-08-25'));
  });

  it('opens a confirmation modal before marcando um horário vago, and only calls marcarHorario after confirming', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });
    listarHorariosVagosMock.mockResolvedValue({ sucesso: true, horarios: [horarioVago] });
    marcarHorarioMock.mockResolvedValue({
      sucesso: true,
      alocacao: { id: 'aloc-1', horarioId: 'hv1', matriculaId: 'm1' },
    });

    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText('Quarta')).toBeTruthy());

    await fireEvent.press(screen.getByRole('button', { name: 'Marcar' }));
    expect(marcarHorarioMock).not.toHaveBeenCalled();
    expect(screen.getByText('Confirmar marcação')).toBeTruthy();

    await fireEvent.press(screen.getAllByRole('button', { name: 'Marcar' })[1]);

    await waitFor(() => expect(marcarHorarioMock).toHaveBeenCalledWith('prof-1', 'hv1'));
  });

  it('shows the Api error message when marcarHorario fails after confirming', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: professorUnico });
    listarHorariosVagosMock.mockResolvedValue({ sucesso: true, horarios: [horarioVago] });
    marcarHorarioMock.mockResolvedValue({ sucesso: false, mensagem: 'Horário lotado.' });

    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText('Quarta')).toBeTruthy());

    await fireEvent.press(screen.getByRole('button', { name: 'Marcar' }));
    await fireEvent.press(screen.getAllByRole('button', { name: 'Marcar' })[1]);

    await waitFor(() => expect(screen.getByText('Horário lotado.')).toBeTruthy());
  });
});
