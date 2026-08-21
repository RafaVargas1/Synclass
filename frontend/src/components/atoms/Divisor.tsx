import { Text, View } from 'react-native';

export type DivisorProps = {
  /** Rótulo central (ex: "ou", entre dois métodos alternativos). Sem ele, mostra o losango decorativo. */
  texto?: string;
};

/**
 * Átomo: linha divisória com marcador central — losango geométrico por
 * padrão (motivo art deco, ver `docs/spec/design-system.md`) ou um rótulo
 * de texto quando os dois lados são alternativas reais (ex: "ou", entre
 * login por código e login por Google) — comunica que são caminhos
 * diferentes pro mesmo objetivo, não uma sequência (Gestalt/agrupamento,
 * `docs/spec/ux-heuristics.md#agrupamento-visual-gestalt`).
 */
export function Divisor({ texto }: DivisorProps) {
  return (
    <View className="w-full flex-row items-center gap-three">
      <View className="h-px flex-1 bg-border dark:bg-dark-border" />
      {texto ? (
        <Text className="text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
          {texto}
        </Text>
      ) : (
        <View
          className="h-2 w-2 bg-primary dark:bg-dark-primary"
          style={{ transform: [{ rotate: '45deg' }] }}
        />
      )}
      <View className="h-px flex-1 bg-border dark:bg-dark-border" />
    </View>
  );
}
