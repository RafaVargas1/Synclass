import { fireEvent, render, screen } from '@testing-library/react-native';

import { MenuNavegacao } from './MenuNavegacao';

const mockUsePathname = jest.fn();
const mockUsePerfilLogado = jest.fn();
const mockUseIsTelaLarga = jest.fn();

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
  useIsTelaLarga: () => mockUseIsTelaLarga(),
}));

describe('MenuNavegacao (issue #77)', () => {
  beforeEach(() => {
    mockUsePathname.mockReset();
    mockUsePerfilLogado.mockReset();
    mockUseIsTelaLarga.mockReset();
    mockUsePerfilLogado.mockReturnValue({ usuarioId: 'abc-123', nome: 'Ana', erro: false });
    mockUseIsTelaLarga.mockReturnValue(true);
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

  it('destaca "Meu perfil" quando a rota ativa é /perfil (issue #129)', async () => {
    mockUsePathname.mockReturnValue('/perfil');

    await render(<MenuNavegacao papeis={['Professor']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />);

    expect(screen.getByRole('link', { name: 'Meu perfil', selected: true })).toBeTruthy();
  });

  it('navega direto entre as seções via Link, sem passar por /painel', async () => {
    mockUsePathname.mockReturnValue('/aluno/historico-frequencia');

    await render(<MenuNavegacao papeis={['Aluno']} papelAtivo="Aluno" onSelecionarPapel={jest.fn()} />);

    // Cada seção é um Link apontando para a própria rota — não para o Painel.
    expect(screen.getByTestId('secao-link-/aluno/historico-frequencia')).toBeTruthy();
    expect(screen.getByTestId('secao-link-/aluno/valor-devido')).toBeTruthy();
    expect(screen.queryByTestId('secao-link-/painel')).toBeNull();
  });

  it('liga seções de Professor ao segmento dinâmico do próprio usuarioId', async () => {
    mockUsePathname.mockReturnValue('/professor/abc-123/horarios');

    await render(<MenuNavegacao papeis={['Professor']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />);

    expect(screen.getByTestId('secao-link-/professor/abc-123/horarios')).toBeTruthy();
    expect(screen.getByTestId('secao-link-/professor/abc-123/alocacoes')).toBeTruthy();
    expect(screen.queryByTestId('secao-link-/painel')).toBeNull();
  });

  it('em viewport larga, o menu fica sempre visível sem exigir toque para abrir', async () => {
    mockUseIsTelaLarga.mockReturnValue(true);
    mockUsePathname.mockReturnValue('/aluno/valor-devido');

    await render(<MenuNavegacao papeis={['Aluno']} papelAtivo="Aluno" onSelecionarPapel={jest.fn()} />);

    // Sem necessidade de acionar botão: as seções já aparecem.
    expect(screen.getByTestId('secao-link-/aluno/valor-devido')).toBeTruthy();
    expect(screen.queryByLabelText('Abrir menu')).toBeNull();
  });

  it('em viewport estreita, o menu começa fechado e abre ao acionar o botão', async () => {
    mockUseIsTelaLarga.mockReturnValue(false);
    mockUsePathname.mockReturnValue('/aluno/valor-devido');

    await render(<MenuNavegacao papeis={['Aluno']} papelAtivo="Aluno" onSelecionarPapel={jest.fn()} />);

    // Fechado: nenhuma seção visível, só o botão de abrir.
    expect(screen.queryByTestId('secao-link-/aluno/valor-devido')).toBeNull();
    expect(screen.getByLabelText('Abrir menu')).toBeTruthy();

    await fireEvent.press(screen.getByLabelText('Abrir menu'));

    expect(screen.getByTestId('secao-link-/aluno/valor-devido')).toBeTruthy();
    expect(screen.getByLabelText('Fechar menu')).toBeTruthy();
  });

  it('gives the abrir/fechar menu button a touch target of at least 44x44 (issue #115)', async () => {
    mockUseIsTelaLarga.mockReturnValue(false);
    mockUsePathname.mockReturnValue('/aluno/valor-devido');

    await render(<MenuNavegacao papeis={['Aluno']} papelAtivo="Aluno" onSelecionarPapel={jest.fn()} />);

    expect(screen.getByLabelText('Abrir menu')).toHaveStyle({ minWidth: 44, minHeight: 44 });
  });

  it('em viewport estreita e menu fechado, o AlternadorDePapel não aparece solto (só junto das seções)', async () => {
    mockUseIsTelaLarga.mockReturnValue(false);
    mockUsePathname.mockReturnValue('/professor/abc-123/horarios');

    await render(
      <MenuNavegacao papeis={['Professor', 'Aluno']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />,
    );

    expect(screen.queryByRole('button', { name: 'Professor' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Aluno' })).toBeNull();

    await fireEvent.press(screen.getByLabelText('Abrir menu'));

    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Aluno', selected: false })).toBeTruthy();
  });

  it('com dois papéis mostra as seções do papel ativo e inclui AlternadorDePapel', async () => {
    mockUsePathname.mockReturnValue('/professor/abc-123/horarios');
    const onSelecionarPapel = jest.fn();

    await render(
      <MenuNavegacao
        papeis={['Professor', 'Aluno']}
        papelAtivo="Professor"
        onSelecionarPapel={onSelecionarPapel}
      />,
    );

    // Seções do papel ativo (Professor) visíveis; as do Aluno não.
    expect(screen.getByTestId('secao-link-/professor/abc-123/horarios')).toBeTruthy();
    expect(screen.queryByTestId('secao-link-/aluno/valor-devido')).toBeNull();
    // AlternadorDePapel presente por haver mais de um papel.
    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Aluno', selected: false })).toBeTruthy();
  });

  it('ao trocar de papel (rerender), a lista de seções exibida muda', async () => {
    mockUsePathname.mockReturnValue('/professor/abc-123/horarios');
    const onSelecionarPapel = jest.fn();

    const { rerender } = await render(
      <MenuNavegacao
        papeis={['Professor', 'Aluno']}
        papelAtivo="Professor"
        onSelecionarPapel={onSelecionarPapel}
      />,
    );

    // Acionar a aba Aluno repassa a troca para o parent.
    await fireEvent.press(screen.getByRole('button', { name: 'Aluno' }));
    expect(onSelecionarPapel).toHaveBeenCalledWith('Aluno');

    // Parent atualiza papelAtivo → menu passa a exibir as seções do Aluno.
    await rerender(
      <MenuNavegacao
        papeis={['Professor', 'Aluno']}
        papelAtivo="Aluno"
        onSelecionarPapel={onSelecionarPapel}
      />,
    );

    expect(screen.getByTestId('secao-link-/aluno/valor-devido')).toBeTruthy();
    expect(screen.queryByTestId('secao-link-/professor/abc-123/horarios')).toBeNull();
  });
});
