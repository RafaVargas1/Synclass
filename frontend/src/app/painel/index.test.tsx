import { render, screen, waitFor } from '@testing-library/react-native';

import { buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';

import PainelScreen from './index';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    useRouter: () => ({ replace: mockRouterReplace }),
    Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
      <Text testID={`link-${href}`}>{children}</Text>
    ),
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

jest.mock('@/lib/api/usuarios', () => ({
  buscarPerfil: jest.fn(),
}));

const useSessaoMock = useSessao as jest.Mock;
const buscarPerfilMock = buscarPerfil as jest.Mock;

describe('PainelScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: false, mensagem: 'erro' });
  });

  it('redirects to /login when there is no saved session', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: null,
      papeis: [],
      papelAtivo: undefined,
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', async () => {
    useSessaoMock.mockReturnValue({
      carregando: true,
      token: null,
      papeis: [],
      papelAtivo: undefined,
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('shows AlternadorDePapel only when there is more than one papel', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Aluno', selected: false })).toBeTruthy();
  });

  it('does not show AlternadorDePapel with a single papel', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('links the Aluno actions to their real screens, without a papel switch', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Aluno'],
      papelAtivo: 'Aluno',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/aluno/historico-frequencia')).toHaveTextContent(
      'Ver histórico de frequência',
    );
    expect(screen.getByTestId('link-/aluno/valor-devido')).toHaveTextContent('Ver valor devido');
    expect(buscarPerfilMock).not.toHaveBeenCalled();
  });

  it('links the Professor actions that need the own usuarioId, resolved from /usuarios/me', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/professor/alunos/cadastro')).toHaveTextContent(
      'Cadastrar Aluno',
    );

    await waitFor(() =>
      expect(screen.getByTestId('link-/professor/prof-1/horarios')).toHaveTextContent(
        'Gerenciar horários',
      ),
    );
    expect(screen.getByTestId('link-/professor/prof-1/alocacoes')).toHaveTextContent(
      'Alocar Aluno em horário',
    );
    expect(screen.getByTestId('link-/professor/prof-1/convites/novo')).toHaveTextContent(
      'Convidar Aluno',
    );
    expect(screen.getByTestId('link-/professor/prof-1/valor-devido')).toHaveTextContent(
      'Ver valor devido',
    );
  });

  it('does not show the usuarioId-dependent Professor actions before /usuarios/me resolves', async () => {
    buscarPerfilMock.mockReturnValue(new Promise(() => {}));
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.queryByText('Gerenciar horários')).toBeNull();
    expect(screen.getByTestId('link-/professor/alunos/cadastro')).toBeTruthy();
  });

  it('switches the actions when the papel changes', async () => {
    const definirPapelAtivo = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Aluno',
      definirPapelAtivo,
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/aluno/historico-frequencia')).toBeTruthy();
    expect(screen.queryByTestId('link-/professor/alunos/cadastro')).toBeNull();
  });

  it('shows a link to the perfil screen', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByText('Meu perfil')).toBeTruthy();
  });
});
