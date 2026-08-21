import { render, screen } from '@testing-library/react-native';

import { MenuNavegacao } from './MenuNavegacao';

const mockUsePathname = jest.fn();
const mockUsePerfilLogado = jest.fn();

jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    usePathname: () => mockUsePathname(),
    useRouter: () => ({ back: jest.fn(), replace: jest.fn() }),
    Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
      <Text testID={`secao-link-${href}`} {...props}>
        {children}
      </Text>
    ),
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: () => ({ token: 'token-jwt' }),
}));

jest.mock('@/lib/usePerfilLogado', () => ({
  usePerfilLogado: () => mockUsePerfilLogado(),
}));

jest.mock('@/lib/useIsTelaLarga', () => ({
  useIsTelaLarga: () => true,
}));

describe('MenuNavegacao (issue #77)', () => {
  beforeEach(() => {
    mockUsePathname.mockReset();
    mockUsePerfilLogado.mockReset();
    mockUsePerfilLogado.mockReturnValue({ usuarioId: 'abc-123', nome: 'Ana', erro: false });
  });

  it('destaca a seção cujo segmento dinâmico casa com a rota ativa', async () => {
    mockUsePathname.mockReturnValue('/professor/abc-123/horarios');

    await render(<MenuNavegacao papeis={['Professor']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />);

    // A seção de horários do Professor vira `/professor/abc-123/horarios` ao
    // substituir [professorId] pelo usuarioId resolvido de /usuarios/me.
    expect(screen.getByRole('link', { name: 'Gerenciar horários', selected: true })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Cadastrar Aluno', selected: false })).toBeTruthy();
  });

  it('não destaca nenhuma seção quando a rota ativa não casa com nenhuma seção', async () => {
    mockUsePathname.mockReturnValue('/outra-rota');

    await render(<MenuNavegacao papeis={['Professor']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />);

    expect(screen.getByRole('link', { name: 'Gerenciar horários', selected: false })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Cadastrar Aluno', selected: false })).toBeTruthy();
  });
});
