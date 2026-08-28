import { fireEvent, render, screen } from '@testing-library/react-native';

import PagamentoPendenteScreen from './pendente';

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

describe('PagamentoPendenteScreen', () => {
  beforeEach(() => {
    mockRouterPush.mockReset();
  });

  it('shows the fixed pending message', async () => {
    await render(<PagamentoPendenteScreen />);

    expect(screen.getByText('Pagamento em processamento — pode levar alguns instantes para confirmar.')).toBeTruthy();
  });

  it('navigates back to /aluno/valor-devido when the button is pressed', async () => {
    await render(<PagamentoPendenteScreen />);

    await fireEvent.press(screen.getByText('Voltar para o valor devido'));

    expect(mockRouterPush).toHaveBeenCalledWith('/aluno/valor-devido');
  });
});
