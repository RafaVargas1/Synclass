import { renderHook, waitFor } from '@testing-library/react-native';

import { useRedirecionarSemSessao } from '@/lib/auth/useRedirecionarSemSessao';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ replace: mockRouterReplace }),
}));

describe('useRedirecionarSemSessao', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
  });

  it('redirects to /login when there is no saved session', async () => {
    renderHook(() => useRedirecionarSemSessao(false, null));

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', () => {
    renderHook(() => useRedirecionarSemSessao(true, null));

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('does not redirect when there is a saved session', () => {
    renderHook(() => useRedirecionarSemSessao(false, 'token-jwt'));

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });
});
