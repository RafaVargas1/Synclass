import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { Topbar } from './Topbar';

const mockBack = jest.fn();
const mockReplace = jest.fn();
const mockCanGoBack = jest.fn();

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: mockBack, replace: mockReplace, canGoBack: mockCanGoBack }),
}));

describe('Topbar', () => {
  beforeEach(() => {
    mockBack.mockReset();
    mockReplace.mockReset();
    mockCanGoBack.mockReset();
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

  it('renders the right-side slot when children are given', async () => {
    await render(
      <Topbar>
        <Text>Sair</Text>
      </Topbar>,
    );

    expect(screen.getByText('Sair')).toBeTruthy();
  });
});
