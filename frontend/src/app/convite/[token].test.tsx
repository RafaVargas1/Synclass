import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { aceitarConvite } from '@/lib/api/convites';

import AceiteConviteScreen from './[token]';

jest.mock('@/lib/api/convites', () => ({
  aceitarConvite: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ token: 'token-1' }),
}));

const aceitarConviteMock = aceitarConvite as jest.Mock;

describe('AceiteConviteScreen', () => {
  beforeEach(() => {
    aceitarConviteMock.mockReset();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    aceitarConviteMock.mockResolvedValue({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
    await render(<AceiteConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
    expect(aceitarConviteMock).toHaveBeenCalledWith({
      token: 'token-1',
      nome: 'João Pedro',
      contato: '(11) 98765-4321',
    });
  });

  it('shows the Api error message without crashing when the Api rejects with a business error', async () => {
    aceitarConviteMock.mockResolvedValue({
      sucesso: false,
      mensagem:
        'O contato informado não corresponde a este convite. Peça um novo link ao Professor.',
    });
    await render(<AceiteConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11900000000');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(
        screen.getByText(
          'O contato informado não corresponde a este convite. Peça um novo link ao Professor.',
        ),
      ).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('shows a dedicated expired state with the action to ask for a new link', async () => {
    aceitarConviteMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Este convite expirou. Peça ao Professor para gerar um novo link.',
    });
    await render(<AceiteConviteScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'João Pedro');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Convite expirado')).toBeTruthy());
    expect(screen.getByText('Peça ao Professor para gerar um novo link de convite.')).toBeTruthy();
  });
});
