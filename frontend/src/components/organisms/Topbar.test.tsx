import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Topbar } from './Topbar';

const mockBack = jest.fn();
const mockReplace = jest.fn();
const mockCanGoBack = jest.fn();
const mockSetOptions = jest.fn();

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: mockBack, replace: mockReplace, canGoBack: mockCanGoBack }),
  useNavigation: () => ({ setOptions: mockSetOptions }),
}));

describe('Topbar', () => {
  beforeEach(() => {
    mockBack.mockReset();
    mockReplace.mockReset();
    mockCanGoBack.mockReset();
    mockSetOptions.mockReset();
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

  it('replaces with /painel when there is no history to go back to', async () => {
    mockCanGoBack.mockReturnValue(false);
    await render(<Topbar titulo="Perfil" />);

    await fireEvent.press(screen.getByLabelText('Voltar'));

    expect(mockReplace).toHaveBeenCalledWith('/painel');
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

  it('sets the browser tab title to the titulo, when given (issue #81)', async () => {
    await render(<Topbar titulo="Valor devido por Aluno" />);

    expect(mockSetOptions).toHaveBeenCalledWith({ title: 'Valor devido por Aluno' });
  });

  it('sets the browser tab title to Synclass when no titulo is given (issue #81)', async () => {
    await render(<Topbar />);

    expect(mockSetOptions).toHaveBeenCalledWith({ title: 'Synclass' });
  });
});
