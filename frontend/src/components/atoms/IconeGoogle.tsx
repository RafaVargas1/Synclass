import { Image } from 'react-native';

export type IconeGoogleProps = {
  tamanho?: number;
};

/**
 * Átomo: logo "G" do Google (issue #113) — usado só pelo BotaoLoginGoogle.
 * Asset em `assets/images/google-g.png` (4 arcos de cor + barra azul,
 * replicando o layout oficial do logo) em vez de embutido como `data:` URI
 * no código-fonte (achado de dev-review, PR #118: base64 inline inflava o
 * bundle JS sem necessidade — um asset de imagem é bundlado/cacheado à
 * parte pelo Metro, igual qualquer outro `.png` do projeto).
 */
export function IconeGoogle({ tamanho = 20 }: IconeGoogleProps) {
  return (
    <Image
      testID="icone-google"
      source={require('../../../assets/images/google-g.png')}
      style={{ width: tamanho, height: tamanho }}
      accessibilityElementsHidden
      importantForAccessibility="no"
    />
  );
}
