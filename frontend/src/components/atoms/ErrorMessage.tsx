import { Text, type TextProps } from 'react-native';

/**
 * Átomo de mensagem de erro acessível (`accessibilityRole="alert"`), usado
 * tanto para erro de campo (`FormField`) quanto para erro geral de
 * formulário (ex: `CadastroProfessorForm`) — mesma aparência nos dois casos,
 * então vive num só lugar em vez de duplicar o JSX/estilo em cada um.
 */
export function ErrorMessage({ className, ...textProps }: TextProps) {
  return (
    <Text
      accessibilityRole="alert"
      className={`text-sm text-error dark:text-dark-error ${className ?? ''}`}
      {...textProps}
    />
  );
}
