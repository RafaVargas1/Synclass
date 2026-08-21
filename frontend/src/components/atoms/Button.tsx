import { Pressable, Text, type PressableProps } from 'react-native';

export type ButtonProps = PressableProps & {
  label: string;
  /**
   * `primario` (default): preenchido, para a ação de maior peso da tela.
   * `secundario`: contorno, sem preenchimento — para uma ação disponível
   * mas que não deve competir visualmente com a primária (issue #111,
   * `docs/spec/ux-heuristics.md#agrupamento-visual-gestalt`).
   */
  variante?: 'primario' | 'secundario';
};

const ClassesPorVariante = {
  primario: 'border-text bg-primary dark:border-dark-text dark:bg-dark-primary',
  secundario: 'border-text bg-transparent dark:border-dark-text',
};

/**
 * Átomo de botão. Não conhece regra de negócio — apenas recebe um label e
 * repassa os demais props de Pressable (ex: onPress, disabled).
 */
export function Button({ label, disabled, variante = 'primario', ...pressableProps }: ButtonProps) {
  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      className={`items-center border-2 px-four py-three active:opacity-80 ${ClassesPorVariante[variante]} ${
        disabled ? 'opacity-40' : ''
      }`}
      {...pressableProps}
    >
      <Text
        className={`text-base font-semibold ${
          variante === 'secundario' ? 'text-text dark:text-dark-text' : 'text-white'
        }`}
      >
        {label}
      </Text>
    </Pressable>
  );
}
