import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { TopbarAutenticada } from './TopbarAutenticada';

const mockUseSessao = jest.fn();
jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: () => mockUseSessao(),
}));

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

// Espião do MenuNavegacao: a integração (item 15) só precisa que o wrapper
// repasse papeis/papelAtivo/onSelecionarPapel vindos de useSessao() para o
// slot menuNavegacao do Topbar. O comportamento do menu em si já é coberto
// por MenuNavegacao.test.tsx.
jest.mock('@/components/organisms/MenuNavegacao', () => {
  const { Pressable, Text: RNText, View } = jest.requireActual('react-native');
  return {
    MenuNavegacao: ({
      papeis,
      papelAtivo,
      onSelecionarPapel,
    }: {
      papeis: string[];
      papelAtivo: string | undefined;
      onSelecionarPapel: (papel: string) => void;
    }) => {
      return (
        <View>
          <RNText>menu-papeis:{papeis.join(',')}</RNText>
          <RNText>menu-papel-ativo:{papelAtivo ?? 'nenhum'}</RNText>
          <Pressable onPress={() => onSelecionarPapel('Aluno')}>
            <RNText>menu-on-selecionar</RNText>
          </Pressable>
        </View>
      );
    },
  };
});

describe('TopbarAutenticada (recebe MenuNavegacao de useSessao)', () => {
  beforeEach(() => {
    mockUseSessao.mockReset();
  });

  it('repassa papeis/papelAtivo/onSelecionarPapel de useSessao para o MenuNavegacao', async () => {
    const definirPapelAtivo = jest.fn();
    mockUseSessao.mockReturnValue({
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo,
    });

    await render(<TopbarAutenticada titulo="Meu perfil" />);

    expect(screen.getByText('menu-papeis:Professor,Aluno')).toBeTruthy();
    expect(screen.getByText('menu-papel-ativo:Professor')).toBeTruthy();

    await fireEvent.press(screen.getByText('menu-on-selecionar'));
    expect(definirPapelAtivo).toHaveBeenCalledWith('Aluno');
  });

  it('mantém titulo e children válidos ao exibir o menu (sem quebrar uso atual)', async () => {
    mockUseSessao.mockReturnValue({
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(
      <TopbarAutenticada titulo="Horários">
        <Text>Sair</Text>
      </TopbarAutenticada>,
    );

    expect(screen.getByRole('header', { name: 'Horários' })).toBeTruthy();
    expect(screen.getByText('Sair')).toBeTruthy();
    expect(screen.getByText('menu-papel-ativo:Professor')).toBeTruthy();
  });
});
