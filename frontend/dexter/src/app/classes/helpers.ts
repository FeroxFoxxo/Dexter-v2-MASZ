export async function timeout(ms: number) {
  console.log("Timeout for " + ms + " ms")
  return new Promise(r => setTimeout(r, ms));
}

export function clearSelection()
{
  let winsel = window.getSelection();
  if (winsel) {winsel.removeAllRanges();}
  let docsel = document.getSelection();
  if (docsel) {docsel.empty();}
}
