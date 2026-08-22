import { act, fireEvent, render, screen } from '@testing-library/react-native';
import { Pressable, Text } from 'react-native';

import { NotificacoesProvider, useNotificacoes } from './contexto-notificacoes';

// Cenários de auto-dismiss, isolados de contexto-notificacoes.test.tsx
// (jest.useFakeTimers nesta suíte, por si só um arquivo separado, evita
// interferir no render() de outros testes — ver comentário no arquivo
// irmão).
function Disparador({ duracaoMs }: { duracaoMs?: number }) {
  const { notificar } = useNotificacoes();
  return (
    <Pressable onPress={() => notificar({ tipo: 'erro', mensagem: 'Falha ao salvar', duracaoMs })}>
      <Text>Disparar</Text>
    </Pressable>
  );
}

describe('NotificacoesProvider (auto-dismiss por duracaoMs)', () => {
  beforeEach(() => {
    jest.useFakeTimers({ doNotFake: ['nextTick'] });
  });

  afterEach(() => {
    jest.clearAllTimers();
    jest.useRealTimers();
  });

  it('does not auto-dismiss an erro by default (needs manual dismissal)', async () => {
    await render(
      <NotificacoesProvider>
        <Disparador />
      </NotificacoesProvider>,
    );

    await fireEvent.press(screen.getByText('Disparar'));
    await act(async () => {
      await jest.advanceTimersByTimeAsync(60_000);
    });

    expect(screen.getByText('Falha ao salvar')).toBeTruthy();
  });

  it('auto-dismisses after the given duracaoMs when provided', async () => {
    await render(
      <NotificacoesProvider>
        <Disparador duracaoMs={3000} />
      </NotificacoesProvider>,
    );

    await fireEvent.press(screen.getByText('Disparar'));
    expect(screen.getByText('Falha ao salvar')).toBeTruthy();

    await act(async () => {
      await jest.runAllTimersAsync();
    });

    expect(screen.queryByText('Falha ao salvar')).toBeNull();
  });
});
