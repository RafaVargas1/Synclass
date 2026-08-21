import { useNavigation } from 'expo-router';
import { useLayoutEffect } from 'react';

/**
 * Define o title da aba do navegador (issue #81) reaproveitando o texto já
 * usado como `titulo` em `Topbar`, sem duplicar por rota. `useNavigation()
 * .setOptions` é a integração suportada pelo React Navigation — o
 * `expo-router` já embute um listener (`useDocumentTitle`) que escreve em
 * `document.title` reagindo a esse mesmo `setOptions`, então não é preciso
 * tocar `document.title` diretamente aqui.
 */
export function useTituloDaAba(titulo: string): void {
  const navigation = useNavigation();

  useLayoutEffect(() => {
    navigation.setOptions({ title: titulo });
  }, [navigation, titulo]);
}
