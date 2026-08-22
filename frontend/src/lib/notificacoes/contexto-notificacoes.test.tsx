import { fireEvent, render, screen } from '@testing-library/react-native';
import { Pressable, Text } from 'react-native';

import { NotificacoesProvider, useNotificacoes } from './contexto-notificacoes';

function Disparador({ duracaoMs }: { duracaoMs?: number }) {
  const { notificar } = useNotificacoes();
  return (
    <Pressable
      onPress={() => notificar({ tipo: 'erro', mensagem: 'Falha ao salvar', duracaoMs })}
    >
      <Text>Disparar</Text>
    </Pressable>
  );
}

// Os cenários de auto-dismiss (fake timers) vivem em
// contexto-notificacoes.timers.test.tsx, isolados neste próprio arquivo —
// jest.useFakeTimers() nesta suíte deixava renders de testes SEGUINTES
// silenciosamente vazios (RNTL `render()` não resolvia o conteúdo),
// mesmo trocando de volta pra jest.useRealTimers() logo depois; isolar
// evita brigar com essa interação em vez de arriscar mais flakiness.
describe('NotificacoesProvider / useNotificacoes', () => {
  it('shows the notification after notificar() is called', async () => {
    await render(
      <NotificacoesProvider>
        <Disparador />
      </NotificacoesProvider>,
    );

    await fireEvent.press(screen.getByText('Disparar'));

    expect(screen.getByText('Falha ao salvar')).toBeTruthy();
  });

  it('dismisses manually via the close button, cancelling any pending auto-dismiss timer', async () => {
    await render(
      <NotificacoesProvider>
        <Disparador duracaoMs={5000} />
      </NotificacoesProvider>,
    );

    await fireEvent.press(screen.getByText('Disparar'));
    await fireEvent.press(screen.getByRole('button', { name: 'Fechar notificação' }));

    expect(screen.queryByText('Falha ao salvar')).toBeNull();
  });

  it('replaces the current notification instead of stacking when notificar() is called again', async () => {
    function DoisDisparadores() {
      const { notificar } = useNotificacoes();
      return (
        <>
          <Pressable onPress={() => notificar({ tipo: 'erro', mensagem: 'Mensagem da primeira notificação' })}>
            <Text>Disparar primeira</Text>
          </Pressable>
          <Pressable onPress={() => notificar({ tipo: 'sucesso', mensagem: 'Mensagem da segunda notificação' })}>
            <Text>Disparar segunda</Text>
          </Pressable>
        </>
      );
    }

    await render(
      <NotificacoesProvider>
        <DoisDisparadores />
      </NotificacoesProvider>,
    );

    await fireEvent.press(screen.getByText('Disparar primeira'));
    await fireEvent.press(screen.getByText('Disparar segunda'));

    expect(screen.queryByText('Mensagem da primeira notificação')).toBeNull();
    expect(screen.getByText('Mensagem da segunda notificação')).toBeTruthy();
  });

  it('throws when useNotificacoes is used outside the provider', async () => {
    const consoleError = jest.spyOn(console, 'error').mockImplementation(() => {});
    function ForaDoProvider() {
      useNotificacoes();
      return null;
    }

    await expect(render(<ForaDoProvider />)).rejects.toThrow(
      'useNotificacoes deve ser usado dentro de um NotificacoesProvider.',
    );
    consoleError.mockRestore();
  });
});
