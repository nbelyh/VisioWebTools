const isoDateRegex = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2}(?:\.\d+)?)(?:Z|(\+|-)([\d|:]*))?$/;

export const parseIsoDate = (key: string, value: any) => {
  if (key && key.indexOf('Date') >= 0 && typeof value === 'string' && isoDateRegex.test(value)) {
    return new Date(value);
  } else {
    return value;
  }
};

export const parseJSON = (text: string | null, defVal?: any) => {
  try {
    if (text)
      return JSON.parse(text, parseIsoDate);
    else
      return defVal;
  } catch (err) {
    if (typeof defVal !== 'undefined') {
      return defVal;
    }
    throw err;
  }
}

export const parseResponseError = async (response: any) => {
  let message: string = '<empty>';
  try {
    message = await response.text();
    return parseJSON(message);
  } catch (err) {
    const error_description = `Unexpected server response: ${message}. ${err}`;
    throw { error_description };
  }
};

export const parseResponseJSON = async (response: any) => {
  let message: string = '<empty>';
  try {
    message = await response.text();
    return parseJSON(message);
  } catch (err) {
    const error_description = `Unexpected server response: ${message}. ${err}`;
    throw { error_description };
  }
};

export async function getErrorMessage(e: any): Promise<string> {
  if (e && typeof e === 'object' && e.hasOwnProperty('isHttpRequestError')) {
    const data = await e.response.json();
    // parse this however you want
    if (typeof data['odata.error'] === 'object') {
      return data['odata.error'].message.value;
    } if (typeof data.error === 'object') {
      return data.error.message;
    } else {
      return stringifyError(e);
    }
  } else {
    return stringifyError(e);
  }
}

export const stringifyError = (err: any): string => {
  if (typeof err === 'string')
    return err;
  if (typeof err === 'object') {
    if (typeof err.error === 'object') {
      return stringifyError(err.error);
    } else if (typeof err.response === 'object' && typeof err.response.toJSON === 'function') {
      return stringifyError(err.response.toJSON()?.body);
    } else {
      return err?.['odata.error']?.message?.value
        ?? err.error_description
        ?? err.error_message
        ?? err.message
        ?? err.error
        ?? JSON.stringify(err);
    }
  } else {
    return 'Unexpected error occured. Please try again later.';
  }
};
