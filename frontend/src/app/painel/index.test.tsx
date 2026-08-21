import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { periodoDoDia } from '@/lib/periodoDoDia';

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

jest.mock('@/lib/periodoDoDia', () => {
  const actual = jest.requireActual('@/lib/periodoDoDia');
  return { ...actual, periodoDoDia: jest.fn() };
});

const useSessaoMock = useSessao as jest.Mock;
const buscarPerfilMock = buscarPerfil as jest.Mock;
const periodoDoDiaMock = periodoDoDia as jest.Mock;

describe('PainelScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: false, mensagem: 'erro' });
    periodoDoDiaMock.mockReturnValue('manha');
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

  it('links the Aluno actions to their real screens and still fetches the perfil for the name', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'aluno-1', nome: 'Bia' });
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
    await waitFor(() => expect(buscarPerfilMock).toHaveBeenCalledTimes(1));
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

  it('marks erro/tentarNovamente when /usuarios/me fails, and recovers on retry', async () => {
    buscarPerfilMock
      .mockResolvedValueOnce({ sucesso: false, mensagem: 'erro' })
      .mockResolvedValueOnce({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() =>
      expect(
        screen.getByText('Não foi possível carregar suas ações de Professor. Tente novamente.'),
      ).toBeTruthy(),
    );

    await fireEvent.press(screen.getByRole('button', { name: 'Tentar novamente' }));

    await waitFor(() =>
      expect(screen.getByTestId('link-/professor/prof-1/horarios')).toHaveTextContent(
        'Gerenciar horários',
      ),
    );
    expect(buscarPerfilMock).toHaveBeenCalledTimes(2);
  });

  it('does not refetch usuarioId when toggling away from and back to Professor', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    const definirPapelAtivo = jest.fn();
    const sessaoBase = {
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      definirPapelAtivo,
    };
    useSessaoMock.mockReturnValue({ ...sessaoBase, papelAtivo: 'Professor' });

    const { rerender } = await render(<PainelScreen />);
    await waitFor(() => expect(buscarPerfilMock).toHaveBeenCalledTimes(1));

    useSessaoMock.mockReturnValue({ ...sessaoBase, papelAtivo: 'Aluno' });
    await rerender(<PainelScreen />);
    useSessaoMock.mockReturnValue({ ...sessaoBase, papelAtivo: 'Professor' });
    await rerender(<PainelScreen />);

    await waitFor(() =>
      expect(screen.getByTestId('link-/professor/prof-1/horarios')).toBeTruthy(),
    );
    expect(buscarPerfilMock).toHaveBeenCalledTimes(1);
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

  it('calls sair when the Sair button is pressed', async () => {
    const sair = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
      sair,
    });

    await render(<PainelScreen />);
    await fireEvent.press(screen.getByText('Sair'));

    expect(sair).toHaveBeenCalled();
  });
});

describe('PainelScreen saudação', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
  });

  it.each([
    ['manha', 'Bom dia'],
    ['tarde', 'Boa tarde'],
    ['noite', 'Boa noite'],
  ])('exibe "{saudacao}, Ana" para o período %s', async (periodo, saudacao) => {
    periodoDoDiaMock.mockReturnValue(periodo);
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText(`${saudacao}, Ana`)).toBeTruthy());
  });

  it('mostra a saudação junto com o AlternadorDePapel quando o perfil resolve', async () => {
    periodoDoDiaMock.mockReturnValue('tarde');
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByText('Boa tarde, Ana')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
  });
});
