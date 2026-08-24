import { Modal, Pressable, Text, View } from 'react-native';

import { AlvoDeToqueMinimo } from '@/theme/tokens';

export type ModalConfirmacaoProps = {
  visivel: boolean;
  titulo: string;
  mensagem: string;
  rotuloConfirmar?: string;
  rotuloCancelar?: string;
  onConfirmar: () => void;
  onFechar: () => void;
  /** Estilo do botão de confirmar para ações que desfazem algo (ex:
   *  cancelar uma aula) — cor de destaque em vez da cor primária. */
  destrutivo?: boolean;
};

/**
 * Organismo: modal genérico de confirmação, reutilizável em qualquer ação
 * que precise de um passo extra antes de efetivar (marcar horário,
 * cancelar aula, etc — ver `/aluno/minhas-aulas.tsx`). Tocar fora do card
 * fecha sem confirmar (Nielsen #3, mesma convenção do overlay mobile do
 * `MenuNavegacao`).
 */
export function ModalConfirmacao({
  visivel,
  titulo,
  mensagem,
  rotuloConfirmar = 'Confirmar',
  rotuloCancelar = 'Cancelar',
  onConfirmar,
  onFechar,
  destrutivo = false,
}: ModalConfirmacaoProps) {
  return (
    <Modal visible={visivel} transparent animationType="fade" onRequestClose={onFechar}>
      <Pressable
        testID="modal-confirmacao-backdrop"
        accessibilityLabel="Fechar sem confirmar"
        onPress={onFechar}
        className="flex-1 items-center justify-center bg-black/40 px-four"
      >
        <Pressable
          testID="modal-confirmacao"
          onPress={(evento) => evento.stopPropagation()}
          className="w-full max-w-[360px] gap-three rounded-medium border border-background-selected bg-background-element p-four dark:border-dark-background-selected dark:bg-dark-background-element"
        >
          <Text accessibilityRole="header" className="text-base font-semibold text-text dark:text-dark-text">
            {titulo}
          </Text>
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">{mensagem}</Text>
          <View className="flex-row flex-wrap justify-end gap-two">
            <Pressable
              accessibilityRole="button"
              onPress={onFechar}
              className="items-center justify-center rounded-small px-three"
              style={AlvoDeToqueMinimo}
            >
              <Text className="font-medium text-text dark:text-dark-text">{rotuloCancelar}</Text>
            </Pressable>
            <Pressable
              accessibilityRole="button"
              onPress={onConfirmar}
              className={`items-center justify-center rounded-small px-three ${
                destrutivo ? 'bg-error dark:bg-dark-error' : 'bg-primary dark:bg-dark-primary'
              }`}
              style={AlvoDeToqueMinimo}
            >
              <Text className="font-medium text-background dark:text-dark-background">{rotuloConfirmar}</Text>
            </Pressable>
          </View>
        </Pressable>
      </Pressable>
    </Modal>
  );
}
