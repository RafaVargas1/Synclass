import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarAlocacoes } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';
import { listarHorarios, TipoMarcacao } from '@/lib/api/horarios';

import MeusAlunosScreen from './alunos';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => {
  const React = jest.requireActual('react');
  return {
    useLocalSearchParams: () => ({ professorId: 'professor-1' }),
    useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
    Link: ({ href, children }: { href: string; children: React.ReactElement }) =>
      React.cloneElement(children, { accessibilityHint: href }),
  };
});

jest.mock('@/lib/api/horarios', () => ({
  listarHorarios: jest.fn(),
  TipoMarcacao: { Livre: 0, Fixo: 1, Hibrido: 2 },
}));
jest.mock('@/lib/api/alunosProvisorios', () => ({ listarAlunosProvisorios: jest.fn() }));
jest.mock('@/lib/api/alocacoes', () => ({ listarAlocacoes: jest.fn() }));

const listarHorariosMock = listarHorarios as jest.Mock;
const listarAlunosProvisoriosMock = listarAlunosProvisorios as jest.Mock;
const listarAlocacoesMock = listarAlocacoes as jest.Mock;

const horarioFixo = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 2,
  tipoMarcacao: TipoMarcacao.Fixo,
};
const alunoAna = { matriculaId: 'a1', nome: 'Ana', identificador: 'ana@x.com' };
const alunoBia = { matriculaId: 'a2', nome: 'Bia', identificador: 'bia@x.com' };

describe('MeusAlunosScreen (issue #160)', () => {
  beforeEach(() => {
    listarHorariosMock.mockReset();
    listarAlunosProvisoriosMock.mockReset();
    listarAlocacoesMock.mockReset();
    listarHorariosMock.mockResolvedValue({ sucesso: true, horarios: [horarioFixo] });
    listarAlunosProvisoriosMock.mockResolvedValue({ sucesso: true, alunos: [] });
    listarAlocacoesMock.mockResolvedValue({ sucesso: true, alocacoes: [] });
  });

  it('shows a loading indicator while the alunos are being fetched', async () => {
    listarAlunosProvisoriosMock.mockReturnValue(new Promise(() => {}));

    await render(<MeusAlunosScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('shows an error message with a retry action when a fetch fails', async () => {
    listarAlunosProvisoriosMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Erro de conexão.',
    });

    await render(<MeusAlunosScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.getByText('Tentar novamente')).toBeTruthy();
  });

  it('retries when the retry button is pressed after a failure', async () => {
    listarAlunosProvisoriosMock
      .mockResolvedValueOnce({ sucesso: false, mensagem: 'Erro de conexão.' })
      .mockResolvedValueOnce({ sucesso: true, alunos: [alunoAna] });

    await render(<MeusAlunosScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    await fireEvent.press(screen.getByText('Tentar novamente'));

    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());
  });

  it('shows an empty state when the professor has no alunos', async () => {
    await render(<MeusAlunosScreen />);

    await waitFor(() =>
      expect(screen.getByText('Nenhum Aluno cadastrado ainda.')).toBeTruthy(),
    );
  });

  it('renders the right cards for alunos with and without horarios', async () => {
    listarAlunosProvisoriosMock.mockResolvedValue({ sucesso: true, alunos: [alunoAna, alunoBia] });
    listarAlocacoesMock.mockResolvedValue({
      sucesso: true,
      alocacoes: [{ id: 'al1', horarioId: 'h1', matriculaId: 'a1', createdAt: '2026-01-01T00:00:00Z' }],
    });

    await render(<MeusAlunosScreen />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());
    expect(screen.getByText('Bia')).toBeTruthy();
    expect(screen.getByText('Terça · 10:00')).toBeTruthy();
    expect(screen.getByText('Sem horário')).toBeTruthy();
  });
});
