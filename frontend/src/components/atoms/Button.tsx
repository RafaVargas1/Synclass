import { Pressable, Text, type PressableProps } from 'react-native';

export type ButtonProps = PressableProps & {
  label: string;
};

/**
 * Átomo de botão. Não conhece regra de negócio — apenas recebe um label e
 * repassa os demais props de Pressable (ex: onPress, disabled).
 */
export function Button({ label, disabled, ...pressableProps }: ButtonProps) {
  return (
    <Pressable
      accessibilityRole="button"
      disabled={disabled}
      className={`items-center rounded-medium bg-primary px-four py-three active:opacity-80 dark:bg-dark-primary ${
        disabled ? 'opacity-40' : ''
      }`}
      {...pressableProps}
    >
      <Text className="text-base font-semibold text-white">{label}</Text>
    </Pressable>
  );
}
