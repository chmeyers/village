

namespace Village.Households;

public enum Role
{
  // The person is the owner of the household.
  HeadOfHousehold,
  // The person is an adult member of the family to the head of the household.
  Family,
  // The person is a child member of the family to the head of the household.
  Child,
  // The person is a servant of the household.
  Servant,
  // The person is a guest of the household.
  Guest
}