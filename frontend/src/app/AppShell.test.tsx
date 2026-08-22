import { render, screen } from '@testing-library/react-native';

import RootLayout from './_layout';

const mockUseSessao = jest.fn();
const mockUseIsTelaLarga = jest.fn();

jest.mock('@/global.css', () => ({}));

jest.mock('expo-router', () => ({
  DarkTheme: { dark: true, colors: {} },
  DefaultTheme: { dark: false, colors: {} },
  ThemeProvider: ({ children }: { children: React.ReactNode }) => children,
  Stack: () => null,
}));

jest.mock('expo-status-bar', () => ({
  StatusBar: () => null,
}));

jest.mock('@/lib/auth/contexto-sessao', () => ({
  SessaoProvider: ({ children }: { children: React.ReactNode }) => children,
  useSessao: () => mockUseSessao(),
}));

jest.mock('@/lib/useIsTelaLarga', () => ({
  useIsTelaLarga: () => mockUseIsTelaLarga(),
}));

// MenuNavegacao depende de usePerfilLogado/usePathname/usuarioId — o
// comportamento dele é coberto pelo próprio MenuNavegacao.test.tsx; aqui
// só precisamos confirmar que a coluna lateral o monta quando deve.
jest.mock('@/components/organisms/MenuNavegacao', () => {
  const { View } = jest.requireActual('react-native');
  return { MenuNavegacao: () => <View testID="menu-navegacao-coluna" /> };
});

describe('AppShell (_layout.tsx, issue #161)', () => {
  beforeEach(() => {
    mockUseSessao.mockReset();
    mockUseIsTelaLarga.mockReset();
    mockUseSessao.mockReturnValue({ token: 'token-jwt', papeis: ['Professor'] });
  });

  it('mostra a coluna lateral com sessão ativa + tela larga', async () => {
    mockUseIsTelaLarga.mockReturnValue(true);

    await render(<RootLayout />);

    expect(screen.getByTestId('coluna-lateral-menu')).toBeTruthy();
    expect(screen.getByTestId('menu-navegacao-coluna')).toBeTruthy();
  });

  it('não mostra a coluna lateral sem sessão (token nulo), mesmo em tela larga', async () => {
    mockUseIsTelaLarga.mockReturnValue(true);
    mockUseSessao.mockReturnValue({ token: null, papeis: [] });

    await render(<RootLayout />);

    expect(screen.queryByTestId('coluna-lateral-menu')).toBeNull();
  });

  it('não mostra a coluna lateral em tela estreita, mesmo com sessão ativa', async () => {
    mockUseIsTelaLarga.mockReturnValue(false);

    await render(<RootLayout />);

    expect(screen.queryByTestId('coluna-lateral-menu')).toBeNull();
  });
});
