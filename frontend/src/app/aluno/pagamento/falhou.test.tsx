import { fireEvent, render, screen } from '@testing-library/react-native';

import PagamentoFalhouScreen from './falhou';

const mockRouterPush = jest.fn();

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, mesmo padrão de
// professor/[professorId]/configuracoes.test.tsx.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useRouter: () => ({ push: mockRouterPush }),
}));

describe('PagamentoFalhouScreen', () => {
  beforeEach(() => {
    mockRouterPush.mockReset();
  });

  it('shows the fixed failure message', async () => {
    await render(<PagamentoFalhouScreen />);

    expect(screen.getByText('Pagamento não concluído — você pode tentar novamente.')).toBeTruthy();
  });

  it('navigates back to /aluno/valor-devido when the button is pressed', async () => {
    await render(<PagamentoFalhouScreen />);

    await fireEvent.press(screen.getByText('Voltar para o valor devido'));

    expect(mockRouterPush).toHaveBeenCalledWith('/aluno/valor-devido');
  });
});
