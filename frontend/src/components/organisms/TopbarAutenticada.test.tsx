import { fireEvent, render, screen } from '@testing-library/react-native';
import { Text } from 'react-native';

import { TopbarAutenticada } from './TopbarAutenticada';

const mockUseSessao = jest.fn();
jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: () => mockUseSessao(),
}));

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
  useNavigation: () => ({ setOptions: jest.fn() }),
}));

const mockTituloDaAba = jest.fn();
jest.mock('@/lib/TituloDaAba', () => ({
  TituloDaAba: (props: { titulo: string }) => {
    mockTituloDaAba(props);
    return null;
  },
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
    mockTituloDaAba.mockReset();
  });

  it('repassa papeis/papelAtivo/onSelecionarPapel de useSessao para o MenuNavegacao', async () => {
    const definirPapelAtivo = jest.fn();
    mockUseSessao.mockReturnValue({
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo,
      sair: jest.fn(),
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
      sair: jest.fn(),
    });

    await render(
      <TopbarAutenticada titulo="Horários">
        <Text>Ação extra</Text>
      </TopbarAutenticada>,
    );

    expect(screen.getByRole('header', { name: 'Horários' })).toBeTruthy();
    expect(screen.getByText('Ação extra')).toBeTruthy();
    expect(screen.getByText('menu-papel-ativo:Professor')).toBeTruthy();
  });

  it('renderiza o botão Sair em toda tela autenticada, chamando sair() de useSessao ao tocar', async () => {
    const sair = jest.fn();
    mockUseSessao.mockReturnValue({
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
      sair,
    });

    await render(<TopbarAutenticada titulo="Horários" />);

    await fireEvent.press(screen.getByText('Sair'));
    expect(sair).toHaveBeenCalled();
  });

  it('dá ao botão Sair um alvo de toque de ao menos 44x44 (issue #115)', async () => {
    mockUseSessao.mockReturnValue({
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
      sair: jest.fn(),
    });

    await render(<TopbarAutenticada titulo="Horários" />);

    expect(screen.getByRole('button', { name: 'Sair' })).toHaveStyle({ minWidth: 44, minHeight: 44 });
  });

  it('repassa tituloDaAba para o Topbar (issue #133)', async () => {
    mockUseSessao.mockReturnValue({
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
      sair: jest.fn(),
    });

    await render(<TopbarAutenticada tituloDaAba="Painel" />);

    expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Painel' });
  });
});
