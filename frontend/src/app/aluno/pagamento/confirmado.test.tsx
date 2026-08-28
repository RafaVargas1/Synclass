import { fireEvent, render, screen } from '@testing-library/react-native';

import PagamentoConfirmadoScreen from './confirmado';

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

describe('PagamentoConfirmadoScreen', () => {
  beforeEach(() => {
    mockRouterPush.mockReset();
  });

  it('shows the fixed success message', async () => {
    await render(<PagamentoConfirmadoScreen />);

    expect(
      screen.getByText('Pagamento concluído — o valor devido é atualizado assim que o pagamento for confirmado.'),
    ).toBeTruthy();
  });

  it('navigates back to /aluno/valor-devido when the button is pressed', async () => {
    await render(<PagamentoConfirmadoScreen />);

    await fireEvent.press(screen.getByText('Voltar para o valor devido'));

    expect(mockRouterPush).toHaveBeenCalledWith('/aluno/valor-devido');
  });
});
