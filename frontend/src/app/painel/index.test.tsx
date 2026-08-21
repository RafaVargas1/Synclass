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

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx e TopbarAutenticada.test.tsx).
// Mockado aqui pra manter este arquivo focado no contrato do próprio
// Painel (conteúdo do corpo).
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
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
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', async () => {
    useSessaoMock.mockReturnValue({
      carregando: true,
      token: null,
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('shows a button (not a text link) to the perfil screen', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/perfil')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Meu perfil' })).toBeTruthy();
  });

  it('calls sair when the Sair button is pressed', async () => {
    const sair = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
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
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-jwt', sair: jest.fn() });
  });

  it.each([
    ['manha', 'Bom dia'],
    ['tarde', 'Boa tarde'],
    ['noite', 'Boa noite'],
  ])('exibe "{saudacao}, Ana" para o período %s', async (periodo, saudacao) => {
    periodoDoDiaMock.mockReturnValue(periodo);

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText(`${saudacao}, Ana`)).toBeTruthy());
  });

  it('não mostra saudação antes do perfil resolver', async () => {
    buscarPerfilMock.mockReturnValue(new Promise(() => {}));

    await render(<PainelScreen />);

    expect(screen.queryByText(/^(Bom dia|Boa tarde|Boa noite),/)).toBeNull();
  });
});
