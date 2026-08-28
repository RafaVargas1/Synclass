import type { Icon as IconeFosforo } from 'phosphor-react-native';
import { Pressable, Text, useColorScheme, View, type PressableProps } from 'react-native';

import { Colors } from '@/theme/tokens';

export type ButtonProps = PressableProps & {
  label: string;
  /**
   * `primario` (default): preenchido, para a ação de maior peso da tela.
   * `secundario`: contorno, sem preenchimento — para uma ação disponível
   * mas que não deve competir visualmente com a primária (issue #111,
   * `docs/spec/ux-heuristics.md#agrupamento-visual-gestalt`).
   */
  variante?: 'primario' | 'secundario';
  /**
   * Ícone opcional (Phosphor), renderizado antes do `label` (issue #202,
   * ex: `icone={CalendarPlus}` em `HorarioVagoCard`). Sem esta prop, o
   * botão renderiza exatamente como antes — nenhuma tela existente muda
   * visualmente. Sempre acompanhado do `label` como texto visível, então
   * fica oculto da árvore de acessibilidade (`accessibilityElementsHidden`)
   * — o nome acessível do botão continua vindo só do `label`.
   */
  icone?: IconeFosforo;
  /** `testID` do ícone opcional, seguindo a convenção `icone-acao-<ação>`
   *  já usada em `HorarioCard`/`MenuNavegacao`. Só tem efeito quando
   *  `icone` é passado. */
  testIDIcone?: string;
};

const ClassesPorVariante = {
  primario: 'border-text bg-primary dark:border-dark-text dark:bg-dark-primary',
  secundario: 'border-text bg-transparent dark:border-dark-text',
};

/**
 * Átomo de botão. Não conhece regra de negócio — apenas recebe um label e
 * repassa os demais props de Pressable (ex: onPress, disabled).
 */
export function Button({
  label,
  disabled,
  variante = 'primario',
  icone: Icone,
  testIDIcone = 'button-icone',
  ...pressableProps
}: ButtonProps) {
  const escuro = useColorScheme() === 'dark';
  const paleta = escuro ? Colors.dark : Colors.light;
  const corDoIcone = variante === 'secundario' ? paleta.text : '#FFFFFF';

  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      className={`flex-row items-center justify-center gap-one border-2 px-four py-three active:opacity-80 ${ClassesPorVariante[variante]} ${
        disabled ? 'opacity-40' : ''
      }`}
      {...pressableProps}
    >
      {Icone ? (
        // `IconProps` do phosphor-react-native não declara props de
        // acessibilidade do RN (`accessibilityElementsHidden`,
        // `importantForAccessibility`) — a View em volta é quem esconde o
        // ícone da árvore de acessibilidade, o `label` continua sendo o
        // único nome acessível do botão.
        <View testID={testIDIcone} accessibilityElementsHidden importantForAccessibility="no">
          <Icone size={20} weight="regular" color={corDoIcone} />
        </View>
      ) : null}
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
