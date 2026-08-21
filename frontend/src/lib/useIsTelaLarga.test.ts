import { renderHook } from '@testing-library/react-native';
import useWindowDimensions from 'react-native/Libraries/Utilities/useWindowDimensions';

import { useIsTelaLarga } from './useIsTelaLarga';

jest.mock('react-native/Libraries/Utilities/useWindowDimensions', () => {
  const useWindowDimensions = jest.fn();
  return { __esModule: true, default: useWindowDimensions };
});

const useWindowDimensionsMock = useWindowDimensions as jest.Mock;

describe('useIsTelaLarga (issue #77)', () => {
  beforeEach(() => {
    useWindowDimensionsMock.mockReset();
  });

  it('retorna true na largura igual ao breakpoint fixo de 1024px', async () => {
    useWindowDimensionsMock.mockReturnValue({ width: 1024 });
    const { result } = await renderHook(() => useIsTelaLarga());
    expect(result.current).toBe(true);
  });

  it('retorna true acima do breakpoint de 1024px', async () => {
    useWindowDimensionsMock.mockReturnValue({ width: 1280 });
    const { result } = await renderHook(() => useIsTelaLarga());
    expect(result.current).toBe(true);
  });

  it('retorna false abaixo do breakpoint de 1024px', async () => {
    useWindowDimensionsMock.mockReturnValue({ width: 375 });
    const { result } = await renderHook(() => useIsTelaLarga());
    expect(result.current).toBe(false);
  });
});
