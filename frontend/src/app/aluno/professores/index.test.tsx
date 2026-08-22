import { render, screen, waitFor } from '@testing-library/react-native';

import { listarVinculosAluno } from '@/lib/api/vinculosAluno';

import MeusProfessoresScreen from './index';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, mesmo padrão de
// professor/[professorId]/alunos.test.tsx.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  Link: ({ children }: { children: React.ReactNode }) => children,
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/vinculosAluno', () => ({ listarVinculosAluno: jest.fn() }));

const listarVinculosAlunoMock = listarVinculosAluno as jest.Mock;

const professorAna = { professorId: 'p1', nome: 'Ana Professora' };

describe('MeusProfessoresScreen (issue #182)', () => {
  beforeEach(() => {
    listarVinculosAlunoMock.mockReset();
  });

  it('shows a loading indicator while the vínculos are being fetched', async () => {
    listarVinculosAlunoMock.mockReturnValue(new Promise(() => {}));

    await render(<MeusProfessoresScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('shows an error message with a retry action when a fetch fails', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<MeusProfessoresScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.getByText('Tentar novamente')).toBeTruthy();
  });

  it('shows a message when the Aluno has no linked Professor yet', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [] });

    await render(<MeusProfessoresScreen />);

    await waitFor(() =>
      expect(screen.getByText('Você ainda não está vinculado a nenhum Professor.')).toBeTruthy(),
    );
  });

  it('lists each linked Professor by nome', async () => {
    listarVinculosAlunoMock.mockResolvedValue({ sucesso: true, vinculos: [professorAna] });

    await render(<MeusProfessoresScreen />);

    await waitFor(() => expect(screen.getByText('Ana Professora')).toBeTruthy());
    expect(screen.getByText('Minhas aulas')).toBeTruthy();
    expect(screen.getByText('Horários disponíveis')).toBeTruthy();
  });
});
