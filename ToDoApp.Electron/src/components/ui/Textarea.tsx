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
          error
            ? "border-red-500 focus:ring-red-500"
            : "border-gray-300 focus:border-primary-500 focus:ring-primary-500",
          "disabled:bg-gray-100 disabled:cursor-not-allowed",
          className
        )}
        {...props}
      />
      {error && errorMessage && (
        <p className="mt-1 text-sm text-red-600" role="alert">
          {errorMessage}
        </p>
      )}
    </div>
  );
};
