import { Pressable, Text, View } from 'react-native';

export type AlternadorDePapelProps = {
  papeis: string[];
  papelAtivo: string | undefined;
  onSelecionarPapel: (papel: string) => void;
};

/**
 * Organismo: alterna o papel ativo (Professor/Aluno) exibido em
 * `app/painel` (issue #4). Não renderiza nada quando o usuário só tem um
 * papel — não há o que alternar (Critério técnico explícito do card).
 */
export function AlternadorDePapel({ papeis, papelAtivo, onSelecionarPapel }: AlternadorDePapelProps) {
  if (papeis.length <= 1) {
    return null;
  }

  return (
    <View accessibilityRole="tablist" className="flex-row gap-one">
      {papeis.map((papel) => (
        <Aba
          key={papel}
          papel={papel}
          selecionado={papel === papelAtivo}
          onPress={() => onSelecionarPapel(papel)}
        />
      ))}
    </View>
  );
}

type AbaProps = { papel: string; selecionado: boolean; onPress: () => void };

function Aba({ papel, selecionado, onPress }: AbaProps) {
  const corDeFundo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-white' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`rounded-small border px-two py-one items-center justify-center ${corDeFundo}`}
      style={{ minWidth: 44, minHeight: 44 }}
    >
      <Text className={corDoTexto}>{papel}</Text>
    </Pressable>
  );
}
