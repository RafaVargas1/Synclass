import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Topbar } from './Topbar';

const mockBack = jest.fn();
const mockReplace = jest.fn();
const mockCanGoBack = jest.fn();
const mockUseIsTelaLarga = jest.fn();
const mockTituloDaAba = jest.fn();

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: mockBack, replace: mockReplace, canGoBack: mockCanGoBack }),
}));

jest.mock('@/lib/useIsTelaLarga', () => ({
  useIsTelaLarga: () => mockUseIsTelaLarga(),
}));

jest.mock('@/lib/TituloDaAba', () => ({
  TituloDaAba: (props: { titulo: string }) => {
    mockTituloDaAba(props);
    return null;
  },
}));

describe('Topbar', () => {
  beforeEach(() => {
    mockBack.mockReset();
    mockReplace.mockReset();
    mockCanGoBack.mockReset();
    mockUseIsTelaLarga.mockReturnValue(false);
    mockTituloDaAba.mockReset();
  });

  it('shows the SYNCLASS mark when no titulo is given', async () => {
    await render(<Topbar />);

    expect(screen.getByText('SYNCLASS')).toBeTruthy();
  });

  it('shows the titulo with a back button instead of the mark', async () => {
    await render(<Topbar titulo="Valor devido por Aluno" />);

    expect(screen.getByText('Valor devido por Aluno')).toBeTruthy();
    expect(screen.queryByText('SYNCLASS')).toBeNull();
  });

  it('shows the "Voltar" label as visible text when a titulo is given (issue #145)', async () => {
    await render(<Topbar titulo="Valor devido por Aluno" />);

    expect(screen.getByText('Voltar')).toBeTruthy();
  });

  it('renders no "Voltar" button when no titulo is given (root screens show the mark)', async () => {
    await render(<Topbar />);

    expect(screen.queryByLabelText('Voltar')).toBeNull();
  });

  it('exposes the titulo as an accessible heading, for screen-reader heading navigation', async () => {
    await render(<Topbar titulo="Valor devido por Aluno" />);

    expect(screen.getByRole('header', { name: 'Valor devido por Aluno' })).toBeTruthy();
  });

  it('exposes the SYNCLASS mark as an accessible heading when no titulo is given', async () => {
    await render(<Topbar />);

    expect(screen.getByRole('header', { name: 'SYNCLASS' })).toBeTruthy();
  });

  it('goes back when there is history to go back to', async () => {
    mockCanGoBack.mockReturnValue(true);
    await render(<Topbar titulo="Perfil" />);

    await fireEvent.press(screen.getByLabelText('Voltar'));

    expect(mockBack).toHaveBeenCalled();
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('replaces with / (Home) when there is no history to go back to (issue #141)', async () => {
    // '/' funciona sem sessão (login) e com sessão — '/painel' travava o
    // usuário num loop de redirecionamento quando não havia sessão ainda.
    mockCanGoBack.mockReturnValue(false);
    await render(<Topbar titulo="Perfil" />);

    await fireEvent.press(screen.getByLabelText('Voltar'));

    expect(mockReplace).toHaveBeenCalledWith('/');
    expect(mockBack).not.toHaveBeenCalled();
  });

  it('gives the back button a touch target of at least 44x44 (Fitts/HIG), not just the small visual chevron', async () => {
    await render(<Topbar titulo="Perfil" />);

    const botaoVoltar = screen.getByLabelText('Voltar');
    expect(botaoVoltar).toHaveStyle({ minWidth: 44, minHeight: 44 });
  });

  it('renders the right-side slot when children are given', async () => {
    await render(
      <Topbar>
        <Text>Sair</Text>
      </Topbar>,
    );

    expect(screen.getByText('Sair')).toBeTruthy();
  });

  it('renders the optional menuNavegacao slot (ex: botão de menu no mobile)', async () => {
    await render(<Topbar menuNavegacao={<Text>Abrir menu</Text>} />);

    expect(screen.getByText('Abrir menu')).toBeTruthy();
  });

  it('mantém titulo e children ao exibir menuNavegacao, sem quebrar o uso atual', async () => {
    await render(
      <Topbar titulo="Meu perfil" menuNavegacao={<Text>Abrir menu</Text>}>
        <Text>Sair</Text>
      </Topbar>,
    );

    expect(screen.getByRole('header', { name: 'Meu perfil' })).toBeTruthy();
    expect(screen.getByText('Abrir menu')).toBeTruthy();
    expect(screen.getByText('Sair')).toBeTruthy();
  });

  it('passes titulo as the tab title when given (issue #81/#133)', async () => {
    await render(<Topbar titulo="Valor devido por Aluno" />);

    expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Valor devido por Aluno' });
  });

  it('falls back to tituloDaAba when given and titulo is absent (issue #133)', async () => {
    await render(<Topbar tituloDaAba="Painel" />);

    expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Painel' });
  });

  it('falls back to the bare Synclass tab title when neither titulo nor tituloDaAba is given', async () => {
    await render(<Topbar />);

    expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Synclass' });
  });

  // O teste "em tela larga, o menu aparece antes do Voltar" (issue #145)
  // foi removido na issue #161: em viewport larga o `Topbar` não renderiza
  // mais `menuNavegacao` (esse branch inteiro deixou de existir) — o menu
  // virou uma coluna lateral persistente montada uma única vez em
  // `AppShell` (`frontend/src/app/_layout.tsx`), fora do `Topbar`. O
  // Voltar agora fica sempre na mesma linha/posição, independente de
  // viewport (ver teste seguinte, adaptado dessa mudança).
  it('em tela larga, o Voltar aparece mesmo sem o menu (a coluna lateral vive fora do Topbar, issue #161)', async () => {
    mockUseIsTelaLarga.mockReturnValue(true);

    await render(<Topbar titulo="Valor devido por Aluno" menuNavegacao={<Text testID="menu-secoes">Seções</Text>} />);

    expect(screen.queryByTestId('menu-secoes')).toBeNull();
    expect(screen.getByText('Voltar')).toBeTruthy();
  });

  it('a linha do cabeçalho no mobile tem z-index elevado, pra o painel fixo do menu não ficar atrás do título/Voltar (issue #146 revisitada)', async () => {
    mockUseIsTelaLarga.mockReturnValue(false);

    await render(
      <Topbar titulo="Valor devido por Aluno" menuNavegacao={<Text testID="menu">Abrir menu</Text>} />,
    );

    const linhaDoCabecalho = screen.getByTestId('menu').parent?.parent;
    expect(linhaDoCabecalho?.props.className).toContain('z-20');
  });
});
