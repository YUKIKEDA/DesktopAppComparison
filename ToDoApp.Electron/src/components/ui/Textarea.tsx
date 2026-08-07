import React from "react";
import clsx from "clsx";

interface TextareaProps
  extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  error?: boolean;
  errorMessage?: string;
}

export const Textarea: React.FC<TextareaProps> = ({
  error,
  errorMessage,
  className,
  ...props
}) => {
  return (
    <div className="w-full">
      <textarea
        className={clsx(
          "w-full px-3 py-2 border rounded-md transition-colors focus-ring",
          "bg-white text-gray-900 placeholder-gray-400 resize-vertical",
          "dark:bg-gray-800 dark:text-gray-100 dark:placeholder-gray-500",
          error
            ? "border-red-500 focus:ring-red-500"
            : "border-gray-300 focus:border-primary-500 focus:ring-primary-500 dark:border-gray-600 dark:focus:border-primary-400 dark:focus:ring-primary-400",
          "disabled:bg-gray-100 disabled:cursor-not-allowed dark:disabled:bg-gray-700",
          className
        )}
        {...props}
      />
      {error && errorMessage && (
        <p className="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
          {errorMessage}
        </p>
      )}
    </div>
  );
};
