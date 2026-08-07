/** Yield to the browser, then run work (keeps UI responsive). */
export function scheduleWork(fn: () => void): () => void {
  if (typeof requestIdleCallback === "function") {
    const id = requestIdleCallback(() => fn(), { timeout: 50 });
    return () => cancelIdleCallback(id);
  }
  const id = window.setTimeout(fn, 0);
  return () => clearTimeout(id);
}

/** Run a sync function after yielding; resolve with its return value. */
export function runInBackground<T>(fn: () => T): Promise<T> {
  return new Promise((resolve, reject) => {
    scheduleWork(() => {
      try {
        resolve(fn());
      } catch (error) {
        reject(error);
      }
    });
  });
}
