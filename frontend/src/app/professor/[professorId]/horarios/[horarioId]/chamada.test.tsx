import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { useLocalSearchParams } from 'expo-router';

import { listarAlocacoes } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';
import { registrarFrequencia } from '@/lib/api/frequencias';

import ChamadaScreen from './chamada';

jest.mock('@/lib/api/alocacoes', () => ({
  listarAlocacoes: jest.fn(),
}));

jest.mock('@/lib/api/alunosProvisorios', () => ({
  listarAlunosProvisorios: jest.fn(),
}));

jest.mock('@/lib/api/frequencias', () => ({
  registrarFrequencia: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: jest.fn(),
}));

const listarAlocacoesMock = listarAlocacoes as jest.Mock;
const listarAlunosProvisoriosMock = listarAlunosProvisorios as jest.Mock;
const registrarFrequenciaMock = registrarFrequencia as jest.Mock;
const useLocalSearchParamsMock = useLocalSearchParams as jest.Mock;

const alunos = [
  { matriculaId: 'matricula-1', nome: 'Ana', identificador: 'ana@x.com' },
  { matriculaId: 'matricula-2', nome: 'Bruno', identificador: 'bruno@x.com' },
];

const alocacoes = [
  {
    id: 'aloc-1',
    horarioId: 'horario-1',
    matriculaId: 'matricula-1',
    createdAt: '2026-01-01T00:00:00Z',
  },
];

describe('ChamadaScreen', () => {
  beforeEach(() => {
    listarAlocacoesMock.mockReset();
    listarAlunosProvisoriosMock.mockReset();
    registrarFrequenciaMock.mockReset();
    useLocalSearchParamsMock.mockReturnValue({
      professorId: 'professor-1',
      horarioId: 'horario-1',
      data: '2026-08-20',
    });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes });
    listarAlunosProvisoriosMock.mockResolvedValue({ sucesso: true, alunos });
  });

  it('lists only the alunos allocated to this horario, defaulting to presente', async () => {
    await render(<ChamadaScreen />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());
    expect(screen.queryByText('Bruno')).toBeNull();
    expect(screen.getByRole('button', { name: 'Presente', selected: true })).toBeTruthy();
  });

  it('shows an error message and does not crash when listing alocacoes fails', async () => {
    listarAlocacoesMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Falha ao carregar Alunos.',
    });
    await render(<ChamadaScreen />);

    await waitFor(() => expect(screen.getByText('Falha ao carregar Alunos.')).toBeTruthy());
  });

  it('toggles an aluno to ausente and sends the batch with the updated status on save', async () => {
    registrarFrequenciaMock.mockResolvedValue({
      sucesso: true,
      registros: [{ matriculaId: 'matricula-1', presente: false, confirmadoPeloAluno: null }],
    });
    await render(<ChamadaScreen />);
    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());

    await fireEvent.press(screen.getByText('Ausente'));
    await fireEvent.press(screen.getByText('Salvar chamada'));

    await waitFor(() =>
      expect(registrarFrequenciaMock).toHaveBeenCalledWith('horario-1', '2026-08-20', [
        { matriculaId: 'matricula-1', presente: false },
      ]),
    );
  });

  it('shows a confirmation message when the save succeeds', async () => {
    registrarFrequenciaMock.mockResolvedValue({
      sucesso: true,
      registros: [{ matriculaId: 'matricula-1', presente: true, confirmadoPeloAluno: null }],
    });
    await render(<ChamadaScreen />);
    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());

    await fireEvent.press(screen.getByText('Salvar chamada'));

    await waitFor(() => expect(screen.getByText('Chamada salva com sucesso.')).toBeTruthy());
  });

  it('shows the Api error message without crashing when saving fails', async () => {
    registrarFrequenciaMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'matriculaId não alocada neste horário.',
    });
    await render(<ChamadaScreen />);
    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());

    await fireEvent.press(screen.getByText('Salvar chamada'));

    await waitFor(() =>
      expect(screen.getByText('matriculaId não alocada neste horário.')).toBeTruthy(),
    );
  });
});
