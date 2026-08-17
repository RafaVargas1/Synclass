import { Linking } from 'react-native';

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { gerarConvite } from '@/lib/api/convites';

import GerarConviteScreen from './novo';

jest.mock('@/lib/api/convites', () => ({
  gerarConvite: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

const gerarConviteMock = gerarConvite as jest.Mock;

describe('GerarConviteScreen', () => {
  beforeEach(() => {
    gerarConviteMock.mockReset();
    jest.spyOn(Linking, 'openURL').mockResolvedValue(true);
  });

  it('shows the invite link and a WhatsApp button when the Api responds with success', async () => {
    gerarConviteMock.mockResolvedValue({
      sucesso: true,
      conviteId: 'convite-1',
      token: 'token-alta-entropia',
      expiraEm: '2026-08-20T00:00:00Z',
    });
    await render(<GerarConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Gerar convite'));

    await waitFor(() => expect(screen.getByText('Convite gerado!')).toBeTruthy());
    expect(screen.getByText(/token-alta-entropia/)).toBeTruthy();
    expect(gerarConviteMock).toHaveBeenCalledWith({
      professorId: 'professor-1',
      contato: '11987654321',
    });
  });

  it('opens the wa.me link when the WhatsApp button is pressed', async () => {
    gerarConviteMock.mockResolvedValue({
      sucesso: true,
      conviteId: 'convite-1',
      token: 'token-1',
      expiraEm: '2026-08-20T00:00:00Z',
    });
    await render(<GerarConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Gerar convite'));
    await waitFor(() => expect(screen.getByText('Convite gerado!')).toBeTruthy());
    await fireEvent.press(screen.getByText('Enviar por WhatsApp'));

    expect(Linking.openURL).toHaveBeenCalledWith(
      expect.stringContaining('https://wa.me/5511987654321'),
    );
  });

  it('shows the Api error message without crashing when the Api rejects the convite', async () => {
    gerarConviteMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este Aluno já está vinculado a este Professor.',
    });
    await render(<GerarConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Gerar convite'));

    await waitFor(() =>
      expect(screen.getByText('Este Aluno já está vinculado a este Professor.')).toBeTruthy(),
    );
    expect(screen.getByText('Gerar convite')).toBeTruthy();
  });
});
