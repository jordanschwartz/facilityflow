import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { quotesApi } from '../../api/quotes';
import LoadingSpinner from '../../components/ui/LoadingSpinner';
import Button from '../../components/ui/Button';
import { CheckCircleIcon } from '@heroicons/react/24/solid';
import { formatCurrency } from '../../utils/formatters';

const schema = z.object({
  price: z.string().min(1, 'Price is required').refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Price must be greater than 0'),
  scopeOfWork: z.string().min(20, 'Please describe your scope of work in detail (min 20 characters)'),
});
type FormData = z.infer<typeof schema>;

export default function QuoteSubmitPage() {
  const { token } = useParams<{ token: string }>();
  const [submitted, setSubmitted] = useState(false);

  const { data, isLoading, error } = useQuery({
    queryKey: ['quote-submit', token],
    queryFn: () => quotesApi.getByToken(token!).then(r => r.data),
    enabled: !!token,
    retry: false,
  });

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const submitMutation = useMutation({
    mutationFn: (formData: FormData) => quotesApi.submitByToken(token!, { price: parseFloat(formData.price), scopeOfWork: formData.scopeOfWork }),
    onSuccess: () => {
      setSubmitted(true);
      toast.success('Quote submitted successfully!');
    },
    onError: () => toast.error('Failed to submit quote. Please try again.'),
  });

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-xl font-semibold text-gray-900 mb-2">Invalid or expired link</h1>
          <p className="text-sm text-gray-500">This quote submission link is not valid or has expired.</p>
        </div>
      </div>
    );
  }

  const sr = data.serviceRequest;
  const existingQuote = data.quote;

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="bg-white border-b border-gray-200 px-6 py-4">
        <div className="max-w-2xl mx-auto flex items-center gap-2">
          <span className="text-gray-900 font-bold text-lg">FacilityFlow</span>
          <span className="text-gray-400">|</span>
          <span className="text-gray-500 text-sm">Vendor Quote Submission</span>
        </div>
      </div>

      <div className="max-w-2xl mx-auto py-12 px-4">
        {submitted || (existingQuote && existingQuote.status !== 'Requested') ? (
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center">
            <CheckCircleIcon className="w-16 h-16 text-green-500 mx-auto mb-4" />
            <h2 className="text-xl font-bold text-gray-900 mb-2">Quote Submitted!</h2>
            <p className="text-gray-600 mb-4">Your quote has been submitted successfully. You'll be notified if your quote is selected.</p>
            {existingQuote && (
              <div className="bg-gray-50 rounded-lg p-4 text-left mt-4">
                <p className="text-sm text-gray-600"><span className="font-medium">Price:</span> {formatCurrency(existingQuote.price ?? 0)}</p>
                <p className="text-sm text-gray-600 mt-1"><span className="font-medium">Status:</span> {existingQuote.status}</p>
              </div>
            )}
          </div>
        ) : (
          <>
            {/* Service Request Info */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mb-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Service Request Details</h2>
              <dl className="space-y-3">
                <div>
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Title</dt>
                  <dd className="mt-1 text-sm font-medium text-gray-900">{sr.title}</dd>
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Location</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.location}</dd>
                  </div>
                  <div>
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">Category</dt>
                    <dd className="mt-1 text-sm text-gray-900">{sr.category}</dd>
                  </div>
                </div>
              </dl>
            </div>

            {/* Quote Form */}
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-base font-semibold text-gray-900 mb-4">Submit Your Quote</h2>
              <form onSubmit={handleSubmit(data => submitMutation.mutate(data))} className="space-y-5">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Your Price (USD)</label>
                  <div className="relative">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 text-sm">$</span>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...register('price')}
                      className="block w-full pl-7 pr-3 py-2 rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border"
                      placeholder="0.00"
                    />
                  </div>
                  {errors.price && <p className="mt-1 text-xs text-red-600">{errors.price.message}</p>}
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Scope of Work</label>
                  <p className="text-xs text-gray-500 mb-2">Describe the work you will perform, timeline, materials, and any relevant details.</p>
                  <textarea
                    {...register('scopeOfWork')}
                    rows={6}
                    className="block w-full rounded-lg border-gray-300 shadow-sm focus:ring-brand-500 focus:border-brand-500 sm:text-sm border px-3 py-2"
                    placeholder="Describe the work you'll perform, estimated timeline, materials included, etc."
                  />
                  {errors.scopeOfWork && <p className="mt-1 text-xs text-red-600">{errors.scopeOfWork.message}</p>}
                </div>

                <Button type="submit" loading={submitMutation.isPending} size="lg" className="w-full">
                  Submit Quote
                </Button>
              </form>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
