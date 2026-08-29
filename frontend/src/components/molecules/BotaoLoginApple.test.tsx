import { Platform } from 'react-native';
import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { BotaoLoginApple } from '@/components/molecules/BotaoLoginApple';
import { loginComApple } from '@/lib/api/auth';
import { obterIdTokenApple } from '@/lib/auth/apple';

jest.mock('@/lib/auth/apple', () => ({
  obterIdTokenApple: jest.fn(),
}));

jest.mock('@/lib/api/auth', () => ({
  loginComApple: jest.fn(),
}));

jest.mock('expo-apple-authentication', () => {
  // Componente fake do botão oficial do SDK — renderizado como uma View com
  // testID fixo para o teste do ramo nativo asserir presença/ausência por
  // plataforma. View é obtido via require('react-native') DENTRO do factory
  // porque o babel-plugin-jest-hoist não permite referenciar variáveis
  // out-of-scope que não comecem com "mock" (ver
  // testing-standards.md#mocks-e-fakes). Nomes em PascalCase para o JSX
  // tratá-los como componentes, não como elementos intrínsecos.
  const MockView = require('react-native').View;
  const MockBotaoAppleNativo = ({ ...rest }: { testID?: string }) => (
    <MockView {...rest} testID="botao-apple-nativo" />
  );
  return {
    AppleAuthenticationButton: MockBotaoAppleNativo,
    AppleAuthenticationButtonType: { SIGN_IN: 'SIGN_IN' },
    AppleAuthenticationButtonStyle: { BLACK: 'BLACK', WHITE: 'WHITE' },
  };
});

const obterIdTokenMock = obterIdTokenApple as jest.Mock;
const loginComAppleMock = loginComApple as jest.Mock;

describe('BotaoLoginApple — ramo web', () => {
  const osOriginal = Platform.OS;
  const onAutenticado = jest.fn();
  const onCadastroPendente = jest.fn();

  beforeEach(() => {
    Platform.OS = 'web';
    obterIdTokenMock.mockReset();
    loginComAppleMock.mockReset();
    onAutenticado.mockReset();
    onCadastroPendente.mockReset();
  });

  afterEach(() => {
    Platform.OS = osOriginal;
  });

  it('chama onAutenticado quando o login Apple tem usuário existente', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComAppleMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Continuar com Apple'));

    await waitFor(() =>
      expect(onAutenticado).toHaveBeenCalledWith({
        token: 'token-jwt',
        nome: 'Maria Silva',
        papeis: ['Professor'],
      }),
    );
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });

  it('chama onCadastroPendente com o e-mail quando não existe usuário', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComAppleMock.mockResolvedValue({
      sucesso: false,
      cadastroPendente: true,
      email: 'naoexiste@exemplo.com',
    });

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Continuar com Apple'));

    await waitFor(() => expect(onCadastroPendente).toHaveBeenCalledWith('naoexiste@exemplo.com'));
    expect(onAutenticado).not.toHaveBeenCalled();
  });

  it('exibe a mensagem de erro quando o e-mail da Apple não é verificado', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComAppleMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O e-mail da conta da Apple não foi verificado. Use uma conta com e-mail verificado.',
    });

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Continuar com Apple'));

    await waitFor(() =>
      expect(
        screen.getByText('O e-mail da conta da Apple não foi verificado. Use uma conta com e-mail verificado.'),
      ).toBeTruthy(),
    );
    expect(onAutenticado).not.toHaveBeenCalled();
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });

  it('não emite callback quando o usuário cancela o fluxo da Apple (idToken null)', async () => {
    obterIdTokenMock.mockResolvedValue(null);

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Continuar com Apple'));

    await waitFor(() => expect(obterIdTokenMock).toHaveBeenCalled());
    expect(loginComAppleMock).not.toHaveBeenCalled();
    expect(onAutenticado).not.toHaveBeenCalled();
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });

  it('mostra "Entrando..." durante o carregamento', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    // Promise controlada manualmente (em vez de mockResolvedValue, que
    // resolveria antes do `waitFor` conseguir observar o estado
    // intermediário "Entrando...") — só resolve quando `resolverLogin` for
    // chamado, garantindo a janela de tempo pra asserção do estado de
    // carregamento.
    let resolverLogin!: (valor: unknown) => void;
    loginComAppleMock.mockReturnValue(
      new Promise((resolve) => {
        resolverLogin = resolve;
      }),
    );

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    // Sem `await` aqui: a Promise de `loginComApple` só resolve depois que
    // `resolverLogin` for chamado abaixo — aguardar `fireEvent.press`
    // pendurava o teste indefinidamente tentando esvaziar a fila de
    // microtasks enquanto essa Promise seguia pendente.
    fireEvent.press(screen.getByText('Continuar com Apple'));

    await waitFor(() => expect(screen.getByText('Entrando...')).toBeTruthy());

    resolverLogin({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });

    await waitFor(() => expect(screen.queryByText('Entrando...')).toBeNull());
  });
});

describe('BotaoLoginApple — ramo nativo e Android', () => {
  const osOriginal = Platform.OS;
  const onAutenticado = jest.fn();
  const onCadastroPendente = jest.fn();

  beforeEach(() => {
    obterIdTokenMock.mockReset();
    loginComAppleMock.mockReset();
    onAutenticado.mockReset();
    onCadastroPendente.mockReset();
  });

  afterEach(() => {
    Platform.OS = osOriginal;
  });

  it('renderiza o AppleAuthenticationButton do SDK e não o botão web em iOS', async () => {
    Platform.OS = 'ios';

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    expect(screen.getByTestId('botao-apple-nativo')).toBeTruthy();
    expect(screen.queryByText('Continuar com Apple')).toBeNull();
  });

  it('não renderiza nada em Android (sem Sign in with Apple)', async () => {
    Platform.OS = 'android';

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    expect(screen.queryByTestId('botao-apple-nativo')).toBeNull();
    expect(screen.queryByText('Continuar com Apple')).toBeNull();
  });

  it('ignora um segundo toque em iOS enquanto o primeiro login ainda está em voo', async () => {
    // AppleAuthenticationButton (diferente do Pressable web) não tem prop
    // `disabled` — a guarda contra duplo toque vive dentro de `handlePress`,
    // então este teste dispara `press` duas vezes antes do primeiro resolver.
    Platform.OS = 'ios';
    let resolverIdToken!: (valor: string | null) => void;
    obterIdTokenMock.mockReturnValue(
      new Promise((resolve) => {
        resolverIdToken = resolve;
      }),
    );
    loginComAppleMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });

    await render(<BotaoLoginApple onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    const botao = screen.getByTestId('botao-apple-nativo');
    fireEvent.press(botao);
    fireEvent.press(botao);
    resolverIdToken('idToken-valido');

    await waitFor(() => expect(onAutenticado).toHaveBeenCalledTimes(1));
    expect(obterIdTokenMock).toHaveBeenCalledTimes(1);
  });
});
