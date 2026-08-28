import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { Linking } from 'react-native';

import { conectarMercadoPago } from '@/lib/api/mercadoPago';

import ConfiguracoesScreen from './configuracoes';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

jest.mock('@/lib/api/mercadoPago', () => ({
  conectarMercadoPago: jest.fn(),
}));

const conectarMercadoPagoMock = conectarMercadoPago as jest.Mock;

describe('ConfiguracoesScreen', () => {
  beforeEach(() => {
    conectarMercadoPagoMock.mockReset();
    jest.spyOn(Linking, 'openURL').mockReset().mockResolvedValue(undefined as never);
  });

  it('opens the authorization url returned by conectarMercadoPago when the button is pressed', async () => {
    conectarMercadoPagoMock.mockResolvedValue({ sucesso: true, url: 'https://auth.mercadopago.com/authorization' });

    await render(<ConfiguracoesScreen />);
    await fireEvent.press(screen.getByText('Conectar conta do Mercado Pago'));

    await waitFor(() => expect(conectarMercadoPagoMock).toHaveBeenCalledWith('professor-1'));
    expect(Linking.openURL).toHaveBeenCalledWith('https://auth.mercadopago.com/authorization');
  });

  it('shows an error message and does not open a url when conectarMercadoPago fails', async () => {
    conectarMercadoPagoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<ConfiguracoesScreen />);
    await fireEvent.press(screen.getByText('Conectar conta do Mercado Pago'));

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(Linking.openURL).not.toHaveBeenCalled();
  });
});
