import { Platform, Pressable, Text, View } from 'react-native';

import { AlvoDeToqueMinimo } from '@/theme/tokens';

export type TipoNotificacao = 'erro' | 'aviso' | 'informacao' | 'sucesso';

export type NotificacaoProps = {
  tipo: TipoNotificacao;
  mensagem: string;
  onFechar: () => void;
};

const ESTILO_POR_TIPO: Record<TipoNotificacao, { corDeFundo: string; corDeTexto: string; rotulo: string }> = {
  erro: {
    corDeFundo: 'border-error bg-error/10 dark:border-dark-error dark:bg-dark-error/10',
    corDeTexto: 'text-error dark:text-dark-error',
    rotulo: 'Erro',
  },
  aviso: {
    corDeFundo: 'border-warning bg-warning/10 dark:border-dark-warning dark:bg-dark-warning/10',
    corDeTexto: 'text-warning dark:text-dark-warning',
    rotulo: 'Aviso',
  },
  informacao: {
    corDeFundo: 'border-info bg-info/10 dark:border-dark-info dark:bg-dark-info/10',
    corDeTexto: 'text-info dark:text-dark-info',
    rotulo: 'Informação',
  },
  sucesso: {
    corDeFundo: 'border-success bg-success/10 dark:border-dark-success dark:bg-dark-success/10',
    corDeTexto: 'text-success dark:text-dark-success',
    rotulo: 'Sucesso',
  },
};

/**
 * Organismo: notificação flutuante (toast) de erro/aviso/informação/sucesso
 * — componente único e reaproveitável pra qualquer feedback assíncrono do
 * sistema (Nielsen #1, visibilidade do status do sistema; Nielsen #9,
 * ajudar o usuário a reconhecer/diagnosticar/recuperar de um erro). `tipo`
 * escolhe a cor entre um conjunto semântico fixo (nunca cor arbitrária —
 * consistência visual entre telas é o próprio ponto de ter um componente
 * único). Cor sozinha nunca é o único sinal (WCAG — não depender só de
 * cor): o `rotulo` textual ("Erro"/"Aviso"/...) sempre acompanha a
 * mensagem para quem não distingue a cor.
 *
 * Sempre com botão de fechar visível, mesmo quando some sozinho depois de
 * `duracaoMs` (`useNotificacoes`, `lib/notificacoes/contexto-notificacoes.tsx`)
 * — controle do usuário sobre quando dispensar (Nielsen #3), e WCAG 2.2.1
 * (conteúdo com prazo precisa poder ser dispensado/estendido pelo usuário,
 * não só por timer automático). `accessibilityRole="alert"` anuncia a
 * chegada da notificação pra leitor de tela sem exigir foco manual.
 */
export function Notificacao({ tipo, mensagem, onFechar }: NotificacaoProps) {
  const estilo = ESTILO_POR_TIPO[tipo];

  return (
    <View
      className={`w-full flex-row items-center gap-three rounded-medium border px-four py-three shadow-lg ${estilo.corDeFundo}`}
    >
      <View className="flex-1">
        <Text className={`text-xs font-semibold ${estilo.corDeTexto}`}>{estilo.rotulo}</Text>
        <Text accessibilityRole="alert" className={`text-sm ${estilo.corDeTexto}`}>
          {mensagem}
        </Text>
      </View>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Fechar notificação"
        onPress={onFechar}
        className="items-center justify-center"
        style={AlvoDeToqueMinimo}
      >
        <Text className={`text-lg font-bold ${estilo.corDeTexto}`}>×</Text>
      </Pressable>
    </View>
  );
}

/**
 * Container de posicionamento fixo (mesmo padrão de `position: fixed` já
 * usado pelo painel mobile de `MenuNavegacao.tsx` — só existe como valor de
 * CSS no web, por isso o guard de `Platform.OS`) — flutua sobre o conteúdo,
 * não desloca layout, mesma convenção de toast/snackbar (Material/HIG).
 * Centralizado horizontalmente, ancorado embaixo (área de safe-area padrão
 * de telas mobile, longe do polegar em cima acidentalmente fechando algo).
 */
export function NotificacaoHost({ children }: { children: React.ReactNode }) {
  return (
    <View
      pointerEvents="box-none"
      className="w-full items-center px-four"
      style={
        Platform.OS === 'web'
          ? ({ position: 'fixed', bottom: 24, left: 0, zIndex: 50 } as never)
          : { position: 'absolute', bottom: 24, left: 0, right: 0, zIndex: 50 }
      }
    >
      <View className="w-full" style={{ maxWidth: 480 }}>
        {children}
      </View>
    </View>
  );
}
